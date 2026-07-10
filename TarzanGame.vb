Option Strict On
Option Explicit On

Imports System.Text
Imports System.Collections.Generic

''' <summary>
''' Logic game Tarzan Rung Xanh Co-op (2 nguoi cung vuot man qua mang).
''' Chuyen the tu du an platformer "Phieu Luu Ong Nuoc" (PlatformGame.vb): giu nguyen
''' vat ly nhay/roi, va cham platform, khoi "?", quai vat, dam dau, nem vat pham, giao thuc
''' mang pipe-delimited (Host authoritative, client chi gui INPUT).
''' Diem moi: co che DU DAY LEO (Vine) - bat buoc phai du day de vuot qua cac khe vuc/song
''' (nhay xuong se mat mang), dong tac bam/tha day dung lai phim Nhay (Space/Up/Z).
''' Nhan vat, quai va vat pham la thiet ke goc lay cam hung tu chu de rung ram Tarzan,
''' khong dung ten/hinh anh nhan vat thuong hieu cua ben thu ba.
''' </summary>
Public Class TarzanGame

    ' ===================== HANG SO THE GIOI =====================
    Public Const TICK_MS As Integer = 33
    Public Const LEVEL_WIDTH_PX As Integer = 6400
    Public Const VIEW_WIDTH_PX As Integer = 960
    Public Const VIEW_HEIGHT_PX As Integer = 540
    Public Const GROUND_Y As Integer = 480

    Public Const GRAVITY As Double = 1.1
    Public Const MAX_FALL_SPEED As Double = 18.0
    Public Const MOVE_SPEED As Double = 5.5
    Public Const JUMP_VELOCITY As Double = -19.0
    Public Const STOMP_BOUNCE_VELOCITY As Double = -12.0
    Public Const PLAYER_W As Integer = 36
    Public Const PLAYER_H As Integer = 48

    Public Const COCONUT_SPEED As Double = 9.0
    Public Const COCONUT_GRAVITY As Double = 0.8
    Public Const COCONUT_BOUNCE_VY As Double = -9.5
    Public Const COCONUT_MAX_BOUNCES As Integer = 4
    Public Const COCONUT_COOLDOWN As Integer = 20
    Public Const COCONUT_MAX_PER_OWNER As Integer = 2

    Public Const ROLL_KICK_SPEED As Double = 8.5
    Public Const BLOCK_W As Integer = 48
    Public Const BLOCK_H As Integer = 48

    ' Kich thuoc quai vat (dung chung cho ca va cham lan ve hinh, xem TarzanGame.vb / Form1.vb)
    Public Const ENEMY_SMALL_W As Integer = 42    ' Leopard, Pangolin
    Public Const ENEMY_SMALL_H As Integer = 48
    Public Const ENEMY_BOSS_W As Integer = 96     ' Gorilla (trum cuoi)
    Public Const ENEMY_BOSS_H As Integer = 96

    Public Const RESPAWN_INVULN_TICKS As Integer = 90
    Public Const HIT_INVULN_TICKS As Integer = 70
    Public Const SHARED_LIVES_START As Integer = 5
    Public Const FRUITS_PER_LIFE As Integer = 100

    Public Const ENEMY_MAX_ALIVE As Integer = 5
    Public Const ENEMY_SPAWN_CHECK_TICKS As Integer = 10

    ' ----- Thong so co che du day leo -----
    Public Const VINE_GRAVITY As Double = 0.55       ' "trong luc" dung cho dao dong con lac
    Public Const VINE_PUMP_ACCEL As Double = 0.0065  ' luc "nhun" khi giu Trai/Phai de tang bien do
    Public Const VINE_DAMPING As Double = 0.999      ' giam dan nang luong (ma sat khong khi nhe)
    Public Const VINE_MAX_ANGLE As Double = 1.15     ' gioi han goc lac (~66 do) tranh xoay vong tron
    Public Const VINE_GRAB_RADIUS As Double = 60.0    ' ban kinh de bam duoc vao day (px)
    Public Const VINE_GRAB_KICK As Double = 0.05      ' toc do goc khoi tao khi vua bam vao day

    ' ===================== ENUM =====================
    Public Enum EnemyType As Byte
        Leopard = 0     ' bao rinh di tuan tren mat dat, dam dau la ha guc ngay
        Pangolin = 1    ' te te: dam dau lan 1 -> cuon tron thanh banh (bat dong); da banh lan
                        ' se tieu diet quai khac tren duong di, cham phai luc dang lan se mat mang
        Gorilla = 2     ' khi dot dau dan (trum cuoi), nhieu mau, thinh thoang quang dua ve nguoi choi
    End Enum

    Public Enum PowerUpType As Byte
        Banana = 0          ' -> WeaponLevel 0->1 (khoe hon, chiu duoc 1 don)
        MagicCoconut = 1    ' -> WeaponLevel ->2 (nem dua duoc)
        HeartOfJungle = 2   ' +1 mang chung
        Fruit = 3           ' +1 trai cay chung (moc FRUITS_PER_LIFE trai se +1 mang)
    End Enum

    Public Enum PlatformKind As Byte
        Ground = 0          ' nen dat rung
        OneWay = 1          ' canh cay: nhay xuyen tu duoi len duoc, chi chan khi roi tu tren
        MysteryCrate = 2    ' hom go bi an: dam dau tu duoi len se bung item ra (dung 1 lan)
        LogPile = 3         ' khuc go chong: chi de trang tri/chan, khong bung item
        HollowStump = 4     ' goc cay rong trang tri, va cham nhu Ground
    End Enum

    ' ===================== STRUCT =====================
    Public Structure PlayerState
        Public X As Double
        Public Y As Double
        Public VelY As Double
        Public FacingRight As Boolean
        Public OnGround As Boolean
        Public Alive As Boolean
        Public IsMoving As Boolean
        Public WeaponLevel As Integer      ' 0 = yeu, 1 = khoe, 2 = nem duoc dua
        Public InvulnTicks As Integer
        Public ShootCooldown As Integer
        Public RespawnTimer As Integer

        ' --- rieng cho co che du day leo ---
        Public OnVine As Boolean
        Public VineIndex As Integer         ' -1 = khong dang o tren day nao
        Public VineAngle As Double          ' goc lac tinh tu phuong thang dung (radian)
        Public VineAngularVel As Double     ' toc do goc
        Public AirMomentumX As Double       ' da ngang con lai sau khi tha day (giam dan)
    End Structure

    Public Structure CoconutState
        Public X As Double
        Public Y As Double
        Public DirX As Double          ' van toc ngang co dinh
        Public VelY As Double          ' van toc doc, bi trong luc + nay
        Public Owner As Integer        ' 0/1 = player nem, -1 = khi dot quang
        Public Bounces As Integer
        Public Active As Boolean
    End Structure

    Public Structure EnemyState
        Public X As Double
        Public Y As Double
        Public VelX As Double
        Public Kind As EnemyType
        Public HP As Integer
        Public Alive As Boolean
        Public FacingRight As Boolean
        Public Curled As Boolean       ' rieng Pangolin: dang cuon tron thanh banh (bat dong hoac lan)
        Public RollingBall As Boolean  ' banh dang bi da lan di (nguy hiem cho quai khac + nguoi choi)
        Public ShootCooldown As Integer
        Public PatrolMinX As Double
        Public PatrolMaxX As Double
    End Structure

    Public Structure PowerUpState
        Public X As Double
        Public Y As Double
        Public VelY As Double
        Public Kind As PowerUpType
        Public Active As Boolean
        Public TtlTicks As Integer
    End Structure

    Public Structure PlatformRect
        Public X As Double
        Public Y As Double
        Public W As Double
        Public H As Double
        Public Kind As PlatformKind
        Public ItemInside As PowerUpType   ' chi dung cho MysteryCrate
        Public Used As Boolean             ' chi dung cho MysteryCrate (da bung item chua)
    End Structure

    ' Day leo: hoan toan tat dinh (giong Platforms), Host/Client tu sinh giong nhau nen
    ' khong can dong bo qua mang, chi can dong bo OnVine/VineIndex cua tung nguoi choi.
    Public Structure VineState
        Public AnchorX As Double
        Public AnchorY As Double
        Public Length As Double
    End Structure

    Private Structure EnemySpawnDef
        Public SpawnAtCamX As Double
        Public X As Double
        Public Y As Double
        Public Kind As EnemyType
        Public HP As Integer
        Public PatrolMinX As Double
        Public PatrolMaxX As Double
        Public Used As Boolean
    End Structure

    Public Structure PlayerInput
        Public Left As Boolean
        Public Right As Boolean
        Public Jump As Boolean      ' cung dung de bam/tha day leo
        Public Shoot As Boolean     ' nem dua
    End Structure

    ' ===================== STATE CHINH =====================
    Public Players(1) As PlayerState
    Public Coconuts As New List(Of CoconutState)
    Public Enemies As New List(Of EnemyState)
    Public PowerUps As New List(Of PowerUpState)
    Public Platforms As New List(Of PlatformRect)
    Public Vines As New List(Of VineState)

    Public CameraX As Double = 0
    Public SharedLives As Integer = SHARED_LIVES_START
    Public SharedFruits As Integer = 0
    Public SharedScore As Integer = 0
    Public GameOver As Boolean = False
    Public Victory As Boolean = False
    Public IsSoloMode As Boolean = False   ' True = chi choi 1 nguoi (Players(1) khong duoc mo phong/ve)

    Private inputs(1) As PlayerInput
    Private prevJumpHeld(1) As Boolean
    Private spawnDefs As New List(Of EnemySpawnDef)
    Private rng As New Random()
    Private tickCount As Integer = 0

    ' ===================== KHOI TAO =====================
    ' Luu y: BuildLevel1() la thuat toan tat dinh (khong random), nen Host va Client
    ' deu tao ra Platforms/Vines/spawnDefs giong het nhau khi khoi tao game rieng. Vi vay
    ' giao thuc mang KHONG can gui lai toan bo dia hinh, chi can dong bo trang thai dong.
    Public Sub New()
        BuildLevel1()
        Players(0) = MakeFreshPlayer(120, GROUND_Y - PLAYER_H)
        Players(1) = MakeFreshPlayer(60, GROUND_Y - PLAYER_H)
    End Sub

    Private Function MakeFreshPlayer(x As Double, y As Double) As PlayerState
        Dim p As New PlayerState()
        p.X = x
        p.Y = y
        p.VelY = 0
        p.FacingRight = True
        p.OnGround = True
        p.Alive = True
        p.WeaponLevel = 0
        p.InvulnTicks = RESPAWN_INVULN_TICKS
        p.ShootCooldown = 0
        p.RespawnTimer = 0
        p.OnVine = False
        p.VineIndex = -1
        p.VineAngle = 0
        p.VineAngularVel = 0
        p.AirMomentumX = 0
        Return p
    End Function

    Private Sub BuildLevel1()
        Platforms.Clear()
        Vines.Clear()
        spawnDefs.Clear()

        ' Cac doan nen dat rung; khoang trong giua cac doan la khe vuc/song - roi xuong day
        ' se mat mang, bat buoc phai du day (Vine) de vuot qua.
        Dim groundSegments As Double()() = New Double()() {
            New Double() {0, 1400},
            New Double() {1500, 2600},
            New Double() {2750, 4200},
            New Double() {4350, LEVEL_WIDTH_PX}
        }
        For Each seg In groundSegments
            AddPlatform(seg(0), GROUND_Y, seg(1) - seg(0), 60, PlatformKind.Ground)
        Next

        ' Canh cay nhieu tang
        AddPlatform(500, 320, 160, 16, PlatformKind.OneWay)
        AddPlatform(900, 260, 160, 16, PlatformKind.OneWay)
        AddPlatform(1900, 320, 200, 16, PlatformKind.OneWay)
        AddPlatform(3100, 300, 180, 16, PlatformKind.OneWay)
        AddPlatform(3500, 240, 160, 16, PlatformKind.OneWay)

        ' Goc cay rong trang tri (chan nhu Ground)
        AddPlatform(1460, GROUND_Y - 48, 60, 48, PlatformKind.HollowStump)
        AddPlatform(2680, GROUND_Y - 64, 60, 64, PlatformKind.HollowStump)
        AddPlatform(4280, GROUND_Y - 40, 60, 40, PlatformKind.HollowStump)

        ' Day leo bat buoc de vuot qua tung khe vuc/song (giua cac doan nen dat o tren)
        AddVine(1450, 130, 280)   ' vuc dau tien: 1400-1500
        AddVine(2675, 130, 280)   ' song thu hai: 2600-2750
        AddVine(4275, 130, 280)   ' vuc thu ba: 4200-4350

        ' Hom go bi an lo lung: dam dau tu duoi len de bung item
        AddMysteryCrate(360, 280, PowerUpType.Fruit)
        AddMysteryCrate(392, 280, PowerUpType.Banana)
        AddMysteryCrate(760, 220, PowerUpType.Fruit)
        AddMysteryCrate(1950, 220, PowerUpType.MagicCoconut)
        AddMysteryCrate(3140, 220, PowerUpType.HeartOfJungle)
        AddMysteryCrate(3700, 180, PowerUpType.Fruit)
        AddMysteryCrate(4620, 280, PowerUpType.MagicCoconut)

        ' Vai vien khuc go trang tri
        AddPlatform(328, 280, 96, BLOCK_H, PlatformKind.LogPile)
        AddPlatform(700, 220, 96, BLOCK_H, PlatformKind.LogPile)

        ' Diem spawn quai theo tien do camera
        AddSpawn(300, 700, GROUND_Y - PLAYER_H, EnemyType.Leopard, 1, 600, 900)
        AddSpawn(700, 1000, GROUND_Y - PLAYER_H, EnemyType.Pangolin, 1, 950, 1300)
        AddSpawn(1200, 1350, GROUND_Y - PLAYER_H, EnemyType.Leopard, 1, 1300, 1600)
        AddSpawn(1800, 2000, GROUND_Y - PLAYER_H, EnemyType.Leopard, 1, 1950, 2300)
        AddSpawn(2200, 2400, GROUND_Y - PLAYER_H, EnemyType.Pangolin, 1, 2350, 2600)
        AddSpawn(2900, 3050, GROUND_Y - PLAYER_H, EnemyType.Leopard, 1, 3050, 3350)
        AddSpawn(3400, 3600, GROUND_Y - PLAYER_H, EnemyType.Pangolin, 1, 3550, 3900)
        AddSpawn(4500, 4700, GROUND_Y - PLAYER_H, EnemyType.Leopard, 1, 4650, 5000)
        ' Trum cuoi man
        AddSpawn(5600, 5900, GROUND_Y - ENEMY_BOSS_H, EnemyType.Gorilla, 6, 5900, 5900)

        ' Trai cay ran rac tren duong (nhat truc tiep, khong can dam khoi)
        PowerUps.Clear()
        AddPowerUp(650, GROUND_Y - 40, PowerUpType.Fruit)
        AddPowerUp(680, GROUND_Y - 40, PowerUpType.Fruit)
        AddPowerUp(2050, GROUND_Y - 40, PowerUpType.Fruit)
        AddPowerUp(3150, 260, PowerUpType.Fruit)
        AddPowerUp(4600, GROUND_Y - 40, PowerUpType.Fruit)
    End Sub

    Private Sub AddPlatform(x As Double, y As Double, w As Double, h As Double, kind As PlatformKind)
        Dim rect As New PlatformRect()
        rect.X = x
        rect.Y = y
        rect.W = w
        rect.H = h
        rect.Kind = kind
        Platforms.Add(rect)
    End Sub

    Private Sub AddMysteryCrate(x As Double, y As Double, item As PowerUpType)
        Dim rect As New PlatformRect()
        rect.X = x
        rect.Y = y
        rect.W = BLOCK_W
        rect.H = BLOCK_H
        rect.Kind = PlatformKind.MysteryCrate
        rect.ItemInside = item
        rect.Used = False
        Platforms.Add(rect)
    End Sub

    Private Sub AddVine(anchorX As Double, anchorY As Double, length As Double)
        Dim v As New VineState()
        v.AnchorX = anchorX
        v.AnchorY = anchorY
        v.Length = length
        Vines.Add(v)
    End Sub

    Private Sub AddSpawn(camTrigger As Double, x As Double, y As Double, kind As EnemyType, hp As Integer, patrolMin As Double, patrolMax As Double)
        Dim d As New EnemySpawnDef()
        d.SpawnAtCamX = camTrigger
        d.X = x
        d.Y = y
        d.Kind = kind
        d.HP = hp
        d.PatrolMinX = patrolMin
        d.PatrolMaxX = patrolMax
        d.Used = False
        spawnDefs.Add(d)
    End Sub

    Private Sub AddPowerUp(x As Double, y As Double, kind As PowerUpType)
        Dim p As New PowerUpState()
        p.X = x
        p.Y = y
        p.VelY = 0
        p.Kind = kind
        p.Active = True
        p.TtlTicks = -1
        PowerUps.Add(p)
    End Sub

    ' ===================== INPUT =====================
    Public Sub SetInput(playerIndex As Integer, inp As PlayerInput)
        If playerIndex < 0 OrElse playerIndex > 1 Then Return
        inputs(playerIndex) = inp
    End Sub

    ' Chi so nguoi choi cuoi cung dang hoat dong: 0 neu choi 1 minh, 1 neu choi 2 nguoi.
    Private ReadOnly Property ActiveMaxPlayerIndex As Integer
        Get
            Return If(IsSoloMode, 0, 1)
        End Get
    End Property

    ' So nguoi choi dang hoat dong (dung tu ben ngoai, vi du Form1 khi ve hinh/HUD).
    Public ReadOnly Property ActivePlayerCount As Integer
        Get
            Return If(IsSoloMode, 1, 2)
        End Get
    End Property

    ' ===================== VONG LAP CHINH (goi tu Timer ben Form1, chi tren HOST) =====================
    Public Sub Tick()
        If GameOver OrElse Victory Then Return
        tickCount += 1

        For i As Integer = 0 To ActiveMaxPlayerIndex
            UpdatePlayer(i)
        Next

        UpdateCoconuts()
        UpdateEnemies()
        CheckPlayerEnemyCollisions()
        UpdatePowerUps()
        CheckSpawns()
        UpdateCamera()
        CheckWinLose()
    End Sub

    Private Sub UpdatePlayer(idx As Integer)
        Dim p As PlayerState = Players(idx)
        Dim inp As PlayerInput = inputs(idx)
        Dim jumpEdge As Boolean = inp.Jump AndAlso Not prevJumpHeld(idx)
        prevJumpHeld(idx) = inp.Jump

        If Not p.Alive Then
            If p.RespawnTimer > 0 Then
                p.RespawnTimer -= 1
                If p.RespawnTimer = 0 Then
                    Dim spawnX As Double = Math.Max(CameraX + 40, 40)
                    p.X = spawnX
                    p.Y = GROUND_Y - PLAYER_H
                    p.VelY = 0
                    p.Alive = True
                    p.InvulnTicks = RESPAWN_INVULN_TICKS
                    p.WeaponLevel = 0
                    p.OnVine = False
                    p.VineIndex = -1
                    p.AirMomentumX = 0
                End If
            End If
            Players(idx) = p
            Return
        End If

        ' --- Dang du day: toan bo vi tri do vat ly con lac quyet dinh, bo qua va cham platform ---
        If p.OnVine Then
            HandleVineSwing(p, inp, jumpEdge)
            If p.InvulnTicks > 0 Then p.InvulnTicks -= 1
            Players(idx) = p
            Return
        End If

        ' --- Thu bam vao day gan do khi vua bam Nhay ---
        If jumpEdge Then
            Dim vIdx As Integer = FindGrabbableVine(p)
            If vIdx >= 0 Then
                GrabVine(p, vIdx)
                Players(idx) = p
                Return
            End If
        End If

        Dim moveX As Double = 0
        If inp.Left Then moveX -= MOVE_SPEED
        If inp.Right Then moveX += MOVE_SPEED
        If moveX > 0 Then p.FacingRight = True
        If moveX < 0 Then p.FacingRight = False
        p.IsMoving = (moveX <> 0)

        If inp.Jump AndAlso p.OnGround Then
            p.VelY = JUMP_VELOCITY
            p.OnGround = False
        End If

        p.VelY += GRAVITY
        If p.VelY > MAX_FALL_SPEED Then p.VelY = MAX_FALL_SPEED

        Dim oldY As Double = p.Y
        Dim newX As Double = p.X + moveX + p.AirMomentumX
        Dim newY As Double = p.Y + p.VelY
        newX = Math.Max(0, Math.Min(newX, CDbl(LEVEL_WIDTH_PX - PLAYER_W)))

        ' Da ngang con lai sau khi tha day (neu co) giam dan ve 0
        p.AirMomentumX *= 0.90
        If Math.Abs(p.AirMomentumX) < 0.05 Then p.AirMomentumX = 0

        ResolvePlatformCollision(newX, newY, oldY, p)

        If p.ShootCooldown > 0 Then p.ShootCooldown -= 1
        If inp.Shoot AndAlso p.ShootCooldown = 0 AndAlso p.WeaponLevel >= 2 Then
            ThrowCoconut(idx, p)
            p.ShootCooldown = COCONUT_COOLDOWN
        End If

        If p.InvulnTicks > 0 Then p.InvulnTicks -= 1

        Players(idx) = p
    End Sub

    ' Tim day leo gan nhat trong tam bam (dua tren vi tri treo tu nhien cua day, goc = 0)
    Private Function FindGrabbableVine(p As PlayerState) As Integer
        Dim cx As Double = p.X + PLAYER_W / 2.0
        Dim cy As Double = p.Y + PLAYER_H / 2.0
        Dim bestIdx As Integer = -1
        Dim bestDistSq As Double = VINE_GRAB_RADIUS * VINE_GRAB_RADIUS
        For i As Integer = 0 To Vines.Count - 1
            Dim v As VineState = Vines(i)
            Dim bx As Double = v.AnchorX
            Dim by As Double = v.AnchorY + v.Length
            Dim ddx As Double = cx - bx
            Dim ddy As Double = cy - by
            Dim distSq As Double = ddx * ddx + ddy * ddy
            If distSq <= bestDistSq Then
                bestDistSq = distSq
                bestIdx = i
            End If
        Next
        Return bestIdx
    End Function

    Private Sub GrabVine(ByRef p As PlayerState, vIdx As Integer)
        Dim v As VineState = Vines(vIdx)
        Dim cx As Double = p.X + PLAYER_W / 2.0
        Dim cy As Double = p.Y + PLAYER_H / 2.0
        Dim dx As Double = cx - v.AnchorX
        Dim dy As Double = cy - v.AnchorY
        p.VineAngle = Math.Atan2(dx, dy)
        Dim dirSign As Double = If(p.FacingRight, 1.0, -1.0)
        If Not p.IsMoving Then dirSign = 0.0
        p.VineAngularVel = VINE_GRAB_KICK * dirSign
        p.OnVine = True
        p.VineIndex = vIdx
        p.VelY = 0
        p.AirMomentumX = 0
    End Sub

    Private Sub HandleVineSwing(ByRef p As PlayerState, inp As PlayerInput, jumpEdge As Boolean)
        Dim v As VineState = Vines(p.VineIndex)

        If jumpEdge Then
            ' Tha day: chuyen dong nang cua con lac thanh van toc bay ra (tiep tuyen voi cung tron)
            Dim vx As Double = v.Length * Math.Cos(p.VineAngle) * p.VineAngularVel
            Dim vy As Double = -v.Length * Math.Sin(p.VineAngle) * p.VineAngularVel

            p.OnVine = False
            p.VineIndex = -1
            p.AirMomentumX = Math.Max(-9.0, Math.Min(9.0, vx))
            p.VelY = Math.Max(-13.0, Math.Min(MAX_FALL_SPEED, vy))
            p.OnGround = False
            Return
        End If

        Dim pumpDir As Double = 0
        If inp.Left Then pumpDir = -1
        If inp.Right Then pumpDir = 1

        Dim angularAccel As Double = -(VINE_GRAVITY / v.Length) * Math.Sin(p.VineAngle)
        p.VineAngularVel += angularAccel
        p.VineAngularVel += pumpDir * VINE_PUMP_ACCEL
        p.VineAngularVel *= VINE_DAMPING
        p.VineAngle += p.VineAngularVel

        If p.VineAngle > VINE_MAX_ANGLE Then
            p.VineAngle = VINE_MAX_ANGLE
            If p.VineAngularVel > 0 Then p.VineAngularVel = 0
        ElseIf p.VineAngle < -VINE_MAX_ANGLE Then
            p.VineAngle = -VINE_MAX_ANGLE
            If p.VineAngularVel < 0 Then p.VineAngularVel = 0
        End If

        p.X = v.AnchorX + v.Length * Math.Sin(p.VineAngle) - PLAYER_W / 2.0
        p.Y = v.AnchorY + v.Length * Math.Cos(p.VineAngle) - PLAYER_H / 2.0
        p.FacingRight = (p.VineAngularVel >= 0)
        p.IsMoving = True
        p.OnGround = False

        If p.Y > VIEW_HEIGHT_PX + 200 Then
            p.OnVine = False
            KillPlayer(p)
        End If
    End Sub

    ' Va cham voi platform (AABB truc doc): xu ly ca roi xuong (tu tren) va dam dau (tu duoi len).
    Private Sub ResolvePlatformCollision(newX As Double, newY As Double, oldY As Double, ByRef p As PlayerState)
        p.X = newX
        Dim landed As Boolean = False

        For pi As Integer = 0 To Platforms.Count - 1
            Dim plat As PlatformRect = Platforms(pi)
            Dim withinX As Boolean = (p.X + PLAYER_W > plat.X) AndAlso (p.X < plat.X + plat.W)
            If Not withinX Then Continue For

            Dim playerBottomOld As Double = oldY + PLAYER_H
            Dim playerBottomNew As Double = newY + PLAYER_H
            Dim playerTopOld As Double = oldY
            Dim playerTopNew As Double = newY

            Dim solidFromTop As Boolean = True ' tat ca cac loai platform hien co deu chan tu tren xuong

            If solidFromTop AndAlso p.VelY >= 0 AndAlso playerBottomOld <= plat.Y + 4 AndAlso playerBottomNew >= plat.Y Then
                newY = plat.Y - PLAYER_H
                p.VelY = 0
                landed = True
            ElseIf plat.Kind <> PlatformKind.OneWay Then
                ' Dam dau tu duoi len (chi ap dung cho khoi dac: Ground/MysteryCrate/LogPile/HollowStump)
                If p.VelY < 0 AndAlso playerTopOld >= plat.Y + plat.H - 4 AndAlso playerTopNew <= plat.Y + plat.H Then
                    newY = plat.Y + plat.H
                    p.VelY = 0.5
                    If plat.Kind = PlatformKind.MysteryCrate AndAlso Not plat.Used Then
                        Dim itemX As Double = plat.X + plat.W / 2.0 - 10
                        SpawnPoppedItem(itemX, plat.Y - 20, plat.ItemInside)
                        plat.Used = True
                        Platforms(pi) = plat
                    End If
                End If
            End If
        Next

        p.Y = newY
        p.OnGround = landed

        If p.Y > VIEW_HEIGHT_PX + 200 Then
            KillPlayer(p)
        End If
    End Sub

    Private Sub SpawnPoppedItem(x As Double, y As Double, kind As PowerUpType)
        Dim pu As New PowerUpState()
        pu.X = x
        pu.Y = y
        pu.VelY = -3.5 ' item bung len roi roi xuong
        pu.Kind = kind
        pu.Active = True
        pu.TtlTicks = If(kind = PowerUpType.Fruit, 60, -1) ' trai cay tu bien mat, item khac nam lai cho nguoi choi nhat
        PowerUps.Add(pu)
    End Sub

    Private Sub KillPlayer(ByRef p As PlayerState)
        If Not p.Alive Then Return
        p.Alive = False
        p.RespawnTimer = 60
        SharedLives -= 1
    End Sub

    ' Nguoi choi mat 1 muc suc manh khi trung don; ve 0 thi moi mat mang han.
    Private Sub DamagePlayer(ByRef p As PlayerState)
        If p.InvulnTicks > 0 Then Return
        If p.WeaponLevel > 0 Then
            p.WeaponLevel -= 1
            p.InvulnTicks = HIT_INVULN_TICKS
        Else
            KillPlayer(p)
        End If
    End Sub

    Private Sub ThrowCoconut(idx As Integer, p As PlayerState)
        Dim activeCount As Integer = 0
        For Each cn In Coconuts
            If cn.Active AndAlso cn.Owner = idx Then activeCount += 1
        Next
        If activeCount >= COCONUT_MAX_PER_OWNER Then Return

        Dim coconut As New CoconutState()
        coconut.X = p.X + PLAYER_W / 2.0
        coconut.Y = p.Y + PLAYER_H / 2.0
        coconut.DirX = If(p.FacingRight, COCONUT_SPEED, -COCONUT_SPEED)
        coconut.VelY = 2.0
        coconut.Owner = idx
        coconut.Bounces = 0
        coconut.Active = True
        Coconuts.Add(coconut)
    End Sub

    Private Sub UpdateCoconuts()
        For i As Integer = Coconuts.Count - 1 To 0 Step -1
            Dim cn As CoconutState = Coconuts(i)
            If Not cn.Active Then
                Coconuts.RemoveAt(i)
                Continue For
            End If

            cn.VelY += COCONUT_GRAVITY
            cn.X += cn.DirX
            cn.Y += cn.VelY

            ' Nay tren mat dat gan dung (dung GROUND_Y lam mat dat tham chieu cho don gian)
            If cn.Y >= GROUND_Y - 4 AndAlso cn.VelY > 0 Then
                cn.Y = GROUND_Y - 4
                cn.VelY = COCONUT_BOUNCE_VY
                cn.Bounces += 1
                If cn.Bounces > COCONUT_MAX_BOUNCES Then cn.Active = False
            End If

            If cn.X < CameraX - 100 OrElse cn.X > CameraX + VIEW_WIDTH_PX + 100 OrElse cn.Y > VIEW_HEIGHT_PX + 100 Then
                cn.Active = False
            End If

            If cn.Active Then
                If cn.Owner >= 0 Then
                    For e As Integer = 0 To Enemies.Count - 1
                        Dim en As EnemyState = Enemies(e)
                        If Not en.Alive Then Continue For
                        If RectHit(cn.X, cn.Y, en.X, en.Y, ENEMY_SMALL_W, ENEMY_SMALL_H) Then
                            cn.Active = False
                            HandleEnemyHitByCoconut(en)
                            Enemies(e) = en
                            Exit For
                        End If
                    Next
                Else
                    For pi As Integer = 0 To ActiveMaxPlayerIndex
                        Dim pl As PlayerState = Players(pi)
                        If Not pl.Alive OrElse pl.InvulnTicks > 0 Then Continue For
                        If RectHit(cn.X, cn.Y, pl.X, pl.Y, PLAYER_W, PLAYER_H) Then
                            cn.Active = False
                            DamagePlayer(pl)
                            Players(pi) = pl
                            Exit For
                        End If
                    Next
                End If
            End If

            Coconuts(i) = cn
        Next
    End Sub

    Private Sub HandleEnemyHitByCoconut(ByRef en As EnemyState)
        en.HP -= 1
        If en.HP <= 0 Then
            en.Alive = False
            SharedScore += 100
            MaybeDropFruit(en.X, en.Y)
        End If
    End Sub

    Private Function RectHit(bx As Double, by As Double, rx As Double, ry As Double, rw As Double, rh As Double) As Boolean
        Return bx >= rx AndAlso bx <= rx + rw AndAlso by >= ry AndAlso by <= ry + rh
    End Function

    Private Sub MaybeDropFruit(x As Double, y As Double)
        If rng.Next(0, 100) < 35 Then
            SpawnPoppedItem(x, y, PowerUpType.Fruit)
        End If
    End Sub

    Private Sub UpdateEnemies()
        For i As Integer = 0 To Enemies.Count - 1
            Dim en As EnemyState = Enemies(i)
            If Not en.Alive Then Continue For

            Select Case en.Kind
                Case EnemyType.Leopard
                    en.X += en.VelX
                    If en.X <= en.PatrolMinX OrElse en.X >= en.PatrolMaxX Then en.VelX = -en.VelX
                    If en.VelX <> 0 Then en.FacingRight = (en.VelX > 0)

                Case EnemyType.Pangolin
                    If en.Curled Then
                        If en.RollingBall Then
                            en.X += en.VelX
                            If en.X <= en.PatrolMinX - 300 OrElse en.X >= en.PatrolMaxX + 300 Then
                                en.VelX = -en.VelX
                            End If
                        End If
                        ' banh dung yen (chua bi da) khong di chuyen
                    Else
                        en.X += en.VelX
                        If en.X <= en.PatrolMinX OrElse en.X >= en.PatrolMaxX Then en.VelX = -en.VelX
                        If en.VelX <> 0 Then en.FacingRight = (en.VelX > 0)
                    End If

                Case EnemyType.Gorilla
                    en.X += en.VelX
                    If en.X <= en.PatrolMinX - 100 OrElse en.X >= en.PatrolMaxX + 100 Then en.VelX = -en.VelX
                    If en.VelX <> 0 Then en.FacingRight = (en.VelX > 0)

                    If en.ShootCooldown > 0 Then
                        en.ShootCooldown -= 1
                    Else
                        Dim target As PlayerState = NearestAlivePlayer(en.X, en.Y)
                        If target.Alive Then
                            Dim coconut As New CoconutState()
                            coconut.X = en.X
                            coconut.Y = en.Y
                            coconut.DirX = If(target.X >= en.X, COCONUT_SPEED * 0.8, -COCONUT_SPEED * 0.8)
                            coconut.VelY = -3.0
                            coconut.Owner = -1
                            coconut.Bounces = 0
                            coconut.Active = True
                            Coconuts.Add(coconut)
                            en.ShootCooldown = 70
                        End If
                    End If
            End Select

            Enemies(i) = en
        Next

        ' Banh (Pangolin cuon tron) dang lan huy diet quai khac khi cham
        For i As Integer = 0 To Enemies.Count - 1
            Dim ball As EnemyState = Enemies(i)
            If Not ball.Alive OrElse Not (ball.Kind = EnemyType.Pangolin AndAlso ball.Curled AndAlso ball.RollingBall) Then Continue For

            For j As Integer = 0 To Enemies.Count - 1
                If i = j Then Continue For
                Dim other As EnemyState = Enemies(j)
                If Not other.Alive Then Continue For
                If RectHit(ball.X, ball.Y, other.X, other.Y, ENEMY_SMALL_W, ENEMY_SMALL_H) Then
                    If other.Kind = EnemyType.Gorilla Then
                        other.HP -= 1
                        If other.HP <= 0 Then
                            other.Alive = False
                            SharedScore += 200
                        End If
                    Else
                        other.Alive = False
                        SharedScore += 100
                    End If
                    Enemies(j) = other
                End If
            Next
        Next
    End Sub

    Private Function NearestAlivePlayer(x As Double, y As Double) As PlayerState
        Dim best As PlayerState = Players(0)
        Dim bestDist As Double = Double.MaxValue
        Dim found As Boolean = False
        For i As Integer = 0 To ActiveMaxPlayerIndex
            If Players(i).Alive Then
                Dim d As Double = Math.Abs(Players(i).X - x)
                If d < bestDist Then
                    bestDist = d
                    best = Players(i)
                    found = True
                End If
            End If
        Next
        If Not found Then
            Dim dead As New PlayerState()
            dead.Alive = False
            Return dead
        End If
        Return best
    End Function

    ' Dam-dau (stomp) va va cham canh (side hit) giua nguoi choi va quai.
    Private Sub CheckPlayerEnemyCollisions()
        For pi As Integer = 0 To ActiveMaxPlayerIndex
            Dim p As PlayerState = Players(pi)
            If Not p.Alive OrElse p.OnVine Then Continue For

            For ei As Integer = 0 To Enemies.Count - 1
                Dim en As EnemyState = Enemies(ei)
                If Not en.Alive Then Continue For

                Dim enW As Double = If(en.Kind = EnemyType.Gorilla, ENEMY_BOSS_W, ENEMY_SMALL_W)
                Dim enH As Double = If(en.Kind = EnemyType.Gorilla, ENEMY_BOSS_H, ENEMY_SMALL_H)
                Dim overlap As Boolean = (p.X + PLAYER_W > en.X) AndAlso (p.X < en.X + enW) AndAlso
                                          (p.Y + PLAYER_H > en.Y) AndAlso (p.Y < en.Y + enH)
                If Not overlap Then Continue For

                Dim isStomp As Boolean = (p.VelY > 0) AndAlso ((p.Y + PLAYER_H) - en.Y < enH * 0.6)

                If isStomp AndAlso en.Kind <> EnemyType.Gorilla AndAlso Not (en.Kind = EnemyType.Pangolin AndAlso en.Curled AndAlso en.RollingBall) Then
                    ' Dam dau -> tieu diet (hoac Pangolin dam lan dau se cuon tron)
                    HandleStomp(en, p)
                    p.VelY = STOMP_BOUNCE_VELOCITY
                    Enemies(ei) = en
                ElseIf en.Kind = EnemyType.Pangolin AndAlso en.Curled Then
                    If en.RollingBall Then
                        If isStomp Then
                            en.RollingBall = False
                            en.VelX = 0
                            p.VelY = STOMP_BOUNCE_VELOCITY
                        Else
                            DamagePlayer(p)
                        End If
                    Else
                        ' Da banh: huong theo vi tri tuong doi cua nguoi choi
                        en.RollingBall = True
                        en.VelX = If(p.X < en.X, ROLL_KICK_SPEED, -ROLL_KICK_SPEED)
                        SharedScore += 20
                    End If
                    Enemies(ei) = en
                ElseIf isStomp AndAlso en.Kind = EnemyType.Gorilla Then
                    en.HP -= 1
                    p.VelY = STOMP_BOUNCE_VELOCITY
                    If en.HP <= 0 Then
                        en.Alive = False
                        SharedScore += 500
                    End If
                    Enemies(ei) = en
                Else
                    DamagePlayer(p)
                End If
            Next

            Players(pi) = p
        Next
    End Sub

    Private Sub HandleStomp(ByRef en As EnemyState, p As PlayerState)
        Select Case en.Kind
            Case EnemyType.Leopard
                en.Alive = False
                SharedScore += 100
                MaybeDropFruit(en.X, en.Y)
            Case EnemyType.Pangolin
                If Not en.Curled Then
                    en.Curled = True
                    en.RollingBall = False
                    en.VelX = 0
                    SharedScore += 100
                End If
        End Select
    End Sub

    Private Sub UpdatePowerUps()
        For i As Integer = PowerUps.Count - 1 To 0 Step -1
            Dim pu As PowerUpState = PowerUps(i)
            If Not pu.Active Then
                PowerUps.RemoveAt(i)
                Continue For
            End If

            ' Trong luc nhe cho item vua bung ra tu hom, roi xuong dat va dung lai
            If pu.VelY <> 0 OrElse pu.Y < GROUND_Y - 20 Then
                pu.VelY += GRAVITY * 0.4
                pu.Y += pu.VelY
                If pu.Y > GROUND_Y - 20 Then
                    pu.Y = GROUND_Y - 20
                    pu.VelY = 0
                End If
            End If

            If pu.TtlTicks > 0 Then
                pu.TtlTicks -= 1
                If pu.TtlTicks = 0 Then pu.Active = False
            End If

            For pi As Integer = 0 To ActiveMaxPlayerIndex
                Dim pl As PlayerState = Players(pi)
                If Not pl.Alive Then Continue For
                If RectHit(pu.X, pu.Y, pl.X, pl.Y, PLAYER_W, PLAYER_H) Then
                    ApplyPowerUp(pl, pu.Kind)
                    Players(pi) = pl
                    pu.Active = False
                End If
            Next

            PowerUps(i) = pu
        Next
    End Sub

    Private Sub ApplyPowerUp(ByRef p As PlayerState, kind As PowerUpType)
        Select Case kind
            Case PowerUpType.Banana
                If p.WeaponLevel < 1 Then p.WeaponLevel = 1
            Case PowerUpType.MagicCoconut
                p.WeaponLevel = 2
            Case PowerUpType.HeartOfJungle
                SharedLives += 1
            Case PowerUpType.Fruit
                SharedFruits += 1
                SharedScore += 10
                If SharedFruits Mod FRUITS_PER_LIFE = 0 Then SharedLives += 1
        End Select
    End Sub

    Private Sub CheckSpawns()
        If tickCount Mod ENEMY_SPAWN_CHECK_TICKS <> 0 Then Return
        Dim aliveCount As Integer = 0
        For Each en In Enemies
            If en.Alive Then aliveCount += 1
        Next
        If aliveCount >= ENEMY_MAX_ALIVE Then Return

        For i As Integer = 0 To spawnDefs.Count - 1
            Dim d As EnemySpawnDef = spawnDefs(i)
            If d.Used Then Continue For
            If CameraX + VIEW_WIDTH_PX >= d.SpawnAtCamX Then
                Dim en As New EnemyState()
                en.X = d.X
                en.Y = d.Y
                en.Kind = d.Kind
                en.HP = d.HP
                en.Alive = True
                en.FacingRight = True
                en.Curled = False
                en.RollingBall = False
                en.ShootCooldown = 40
                en.PatrolMinX = d.PatrolMinX
                en.PatrolMaxX = d.PatrolMaxX
                en.VelX = If(d.Kind = EnemyType.Gorilla, 0.8, 1.0)
                Enemies.Add(en)
                d.Used = True
                spawnDefs(i) = d
            End If
        Next
    End Sub

    Private Sub UpdateCamera()
        Dim maxX As Double = CameraX
        For i As Integer = 0 To ActiveMaxPlayerIndex
            If Players(i).Alive Then
                Dim desired As Double = Players(i).X - 240
                If desired > maxX Then maxX = desired
            End If
        Next
        maxX = Math.Min(maxX, CDbl(LEVEL_WIDTH_PX - VIEW_WIDTH_PX))
        CameraX = Math.Max(CameraX, Math.Max(0.0, maxX))

        For i As Integer = 0 To ActiveMaxPlayerIndex
            Dim p As PlayerState = Players(i)
            If p.Alive AndAlso p.X < CameraX Then
                p.X = CameraX
                Players(i) = p
            End If
        Next
    End Sub

    Private Sub CheckWinLose()
        If SharedLives <= 0 Then
            Dim bothDead As Boolean = (Not Players(0).Alive) AndAlso (IsSoloMode OrElse Not Players(1).Alive)
            If bothDead Then GameOver = True
            Return
        End If

        Dim bossDead As Boolean = True
        Dim bossExists As Boolean = False
        For Each en In Enemies
            If en.Kind = EnemyType.Gorilla Then
                bossExists = True
                If en.Alive Then bossDead = False
            End If
        Next
        If bossExists AndAlso bossDead Then Victory = True
    End Sub

    ' ===================== SERIALIZE / DESERIALIZE (giao thuc mang) =====================
    ' STATE|camX|lives|fruits|score|gameOver|victory|p0|p1|nCoconuts|c1;c2;...|nEnemies|e1;e2;...|nPowerups|u1;u2;...|usedCrateBits
    Public Function SerializeState() As String
        Dim sb As New StringBuilder()
        sb.Append("STATE|")
        sb.Append(CameraX.ToString("F1")).Append("|")
        sb.Append(SharedLives.ToString()).Append("|")
        sb.Append(SharedFruits.ToString()).Append("|")
        sb.Append(SharedScore.ToString()).Append("|")
        sb.Append(If(GameOver, "1", "0")).Append("|")
        sb.Append(If(Victory, "1", "0")).Append("|")
        sb.Append(SerializePlayer(Players(0))).Append("|")
        sb.Append(SerializePlayer(Players(1))).Append("|")

        sb.Append(Coconuts.Count.ToString()).Append("|")
        For i As Integer = 0 To Coconuts.Count - 1
            If i > 0 Then sb.Append(";")
            Dim cn As CoconutState = Coconuts(i)
            sb.Append(String.Format(Globalization.CultureInfo.InvariantCulture, "{0:F1},{1:F1},{2:F1},{3:F1},{4}", cn.X, cn.Y, cn.DirX, cn.VelY, cn.Owner))
        Next
        sb.Append("|")

        sb.Append(Enemies.Count.ToString()).Append("|")
        For i As Integer = 0 To Enemies.Count - 1
            If i > 0 Then sb.Append(";")
            Dim en As EnemyState = Enemies(i)
            sb.Append(String.Format(Globalization.CultureInfo.InvariantCulture, "{0:F1},{1:F1},{2},{3},{4},{5},{6},{7}",
                en.X, en.Y, CInt(en.Kind), en.HP, If(en.Alive, 1, 0), If(en.FacingRight, 1, 0), If(en.Curled, 1, 0), If(en.RollingBall, 1, 0)))
        Next
        sb.Append("|")

        sb.Append(PowerUps.Count.ToString()).Append("|")
        For i As Integer = 0 To PowerUps.Count - 1
            If i > 0 Then sb.Append(";")
            Dim pu As PowerUpState = PowerUps(i)
            sb.Append(String.Format(Globalization.CultureInfo.InvariantCulture, "{0:F1},{1:F1},{2}", pu.X, pu.Y, CInt(pu.Kind)))
        Next
        sb.Append("|")

        Dim bits As New StringBuilder()
        For Each plat In Platforms
            bits.Append(If(plat.Kind = PlatformKind.MysteryCrate AndAlso plat.Used, "1"c, "0"c))
        Next
        sb.Append(bits.ToString())

        Return sb.ToString()
    End Function

    Private Function SerializePlayer(p As PlayerState) As String
        Return String.Format(Globalization.CultureInfo.InvariantCulture, "{0:F1},{1:F1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
            p.X, p.Y,
            If(p.FacingRight, 1, 0),
            If(p.OnGround, 1, 0),
            If(p.Alive, 1, 0),
            p.WeaponLevel,
            p.InvulnTicks,
            p.RespawnTimer,
            If(p.IsMoving, 1, 0),
            If(p.OnVine, 1, 0),
            p.VineIndex)
    End Function

    Public Sub ApplyStateLine(line As String)
        Dim parts As String() = line.Split("|"c)
        If parts.Length < 12 Then Return
        If parts(0) <> "STATE" Then Return

        Dim ic As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture
        CameraX = Double.Parse(parts(1), ic)
        SharedLives = Integer.Parse(parts(2))
        SharedFruits = Integer.Parse(parts(3))
        SharedScore = Integer.Parse(parts(4))
        GameOver = (parts(5) = "1")
        Victory = (parts(6) = "1")
        Players(0) = ParsePlayer(parts(7))
        Players(1) = ParsePlayer(parts(8))

        Dim nCoconuts As Integer = Integer.Parse(parts(9))
        Coconuts.Clear()
        If nCoconuts > 0 Then
            For Each item In parts(10).Split(";"c)
                Coconuts.Add(ParseCoconut(item))
            Next
        End If

        Dim nEnemies As Integer = Integer.Parse(parts(11))
        Enemies.Clear()
        If nEnemies > 0 AndAlso parts.Length > 12 Then
            For Each item In parts(12).Split(";"c)
                Enemies.Add(ParseEnemy(item))
            Next
        End If

        If parts.Length > 14 Then
            Dim nPowerups As Integer = Integer.Parse(parts(13))
            PowerUps.Clear()
            If nPowerups > 0 Then
                For Each item In parts(14).Split(";"c)
                    PowerUps.Add(ParsePowerUp(item))
                Next
            End If
        End If

        If parts.Length > 15 Then
            Dim bits As String = parts(15)
            For i As Integer = 0 To Math.Min(bits.Length, Platforms.Count) - 1
                If bits(i) = "1"c Then
                    Dim plat As PlatformRect = Platforms(i)
                    plat.Used = True
                    Platforms(i) = plat
                End If
            Next
        End If
    End Sub

    Private Function ParsePlayer(s As String) As PlayerState
        Dim f As String() = s.Split(","c)
        Dim ic As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture
        Dim p As New PlayerState()
        p.X = Double.Parse(f(0), ic)
        p.Y = Double.Parse(f(1), ic)
        p.FacingRight = (f(2) = "1")
        p.OnGround = (f(3) = "1")
        p.Alive = (f(4) = "1")
        p.WeaponLevel = Integer.Parse(f(5))
        p.InvulnTicks = Integer.Parse(f(6))
        p.RespawnTimer = Integer.Parse(f(7))
        p.IsMoving = If(f.Length > 8, f(8) = "1", False)
        p.OnVine = If(f.Length > 9, f(9) = "1", False)
        p.VineIndex = If(f.Length > 10, Integer.Parse(f(10)), -1)
        Return p
    End Function

    Private Function ParseCoconut(s As String) As CoconutState
        Dim f As String() = s.Split(","c)
        Dim ic As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture
        Dim cn As New CoconutState()
        cn.X = Double.Parse(f(0), ic)
        cn.Y = Double.Parse(f(1), ic)
        cn.DirX = Double.Parse(f(2), ic)
        cn.VelY = Double.Parse(f(3), ic)
        cn.Owner = Integer.Parse(f(4))
        cn.Active = True
        Return cn
    End Function

    Private Function ParseEnemy(s As String) As EnemyState
        Dim f As String() = s.Split(","c)
        Dim ic As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture
        Dim en As New EnemyState()
        en.X = Double.Parse(f(0), ic)
        en.Y = Double.Parse(f(1), ic)
        en.Kind = CType(Integer.Parse(f(2)), EnemyType)
        en.HP = Integer.Parse(f(3))
        en.Alive = (f(4) = "1")
        en.FacingRight = If(f.Length > 5, f(5) = "1", True)
        en.Curled = If(f.Length > 6, f(6) = "1", False)
        en.RollingBall = If(f.Length > 7, f(7) = "1", False)
        Return en
    End Function

    Private Function ParsePowerUp(s As String) As PowerUpState
        Dim f As String() = s.Split(","c)
        Dim ic As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture
        Dim pu As New PowerUpState()
        pu.X = Double.Parse(f(0), ic)
        pu.Y = Double.Parse(f(1), ic)
        pu.Kind = CType(Integer.Parse(f(2)), PowerUpType)
        pu.Active = True
        Return pu
    End Function

    Public Shared Function SerializeInput(inp As PlayerInput) As String
        Return String.Format("INPUT|{0}|{1}|{2}|{3}",
            If(inp.Left, 1, 0), If(inp.Right, 1, 0),
            If(inp.Jump, 1, 0), If(inp.Shoot, 1, 0))
    End Function

    Public Shared Function ParseInput(line As String) As PlayerInput
        Dim inp As New PlayerInput()
        Dim parts As String() = line.Split("|"c)
        If parts.Length < 5 OrElse parts(0) <> "INPUT" Then Return inp
        inp.Left = (parts(1) = "1")
        inp.Right = (parts(2) = "1")
        inp.Jump = (parts(3) = "1")
        inp.Shoot = (parts(4) = "1")
        Return inp
    End Function

End Class
