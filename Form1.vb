Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.IO

Public Class Form1
    Inherits Form

    Private WithEvents TickTimer As New Timer()
    Private net As NetworkPeer
    Private game As New TarzanGame()

    Private isHost As Boolean = False
    Private isConnected As Boolean = False
    Private localPlayerIndex As Integer = 0 ' host = 0, client = 1
    Private frameCounter As Integer = 0

    Private keyLeft As Boolean
    Private keyRight As Boolean
    Private keyJump As Boolean     ' cung dung de bam/tha day leo
    Private keyShoot As Boolean    ' nem dua

    Private Structure SpriteSheet
        Public Sheet As Bitmap
        Public FrameW As Integer
        Public FrameH As Integer
        Public FrameCount As Integer
    End Structure

    Private sheetPlayer0 As SpriteSheet
    Private sheetPlayer1 As SpriteSheet
    Private sheetLeopard As SpriteSheet
    Private sheetGorilla As SpriteSheet

    ' Ten bien giu nguyen tu du an goc de tai su dung dung file PNG da co san (Assets/*),
    ' chi doi y nghia: player0/1 = Tarzan & ban dong hanh, enemy_soldier = bao rinh,
    ' enemy_shelled/enemy_shell = te te (Pangolin) luc di / luc cuon banh, enemy_boss = khi dot,
    ' tile_ground = nen dat rung, tile_pipe = goc cay rong, tile_questionblock = hom go bi an,
    ' bullet_player/enemy = trai dua nem, powerup_banana/coconut = chuoi/dua than,
    ' powerup_life = trai tim rung, powerup_fruit = trai cay.
    Private spPlayer0 As Bitmap
    Private spPlayer0Walk2 As Bitmap
    Private spPlayer0Jump As Bitmap
    Private spPlayer1 As Bitmap
    Private spPlayer1Walk2 As Bitmap
    Private spPlayer1Jump As Bitmap
    Private spLeopard As Bitmap
    Private spLeopardWalk2 As Bitmap
    Private spPangolin As Bitmap
    Private spPangolinBall As Bitmap
    Private spPangolinBallRoll As Bitmap
    Private spGorilla As Bitmap
    Private spGorillaWalk2 As Bitmap
    Private spGround As Bitmap
    Private spMysteryCrate As Bitmap
    Private spHollowStump As Bitmap
    Private spCoconutPlayer As Bitmap
    Private spCoconutEnemy As Bitmap
    Private spPowerBanana As Bitmap
    Private spPowerMagicCoconut As Bitmap
    Private spPowerHeart As Bitmap
    Private spPowerFruit As Bitmap
    Private spBackground As Bitmap

    Private lblStatus As New Label()
    Private btnSolo As New Button()
    Private btnHost As New Button()
    Private btnJoin As New Button()
    Private txtIp As New TextBox()
    Private pnlMenu As New Panel()

    Public Sub New()
        Me.Text = "Tarzan Rung Xanh - Co-op Online"
        Me.ClientSize = New Size(TarzanGame.VIEW_WIDTH_PX, TarzanGame.VIEW_HEIGHT_PX)
        Me.DoubleBuffered = True
        Me.KeyPreview = True
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.StartPosition = FormStartPosition.CenterScreen

        LoadSpritesIfExist()
        BuildMenuUI()

        TickTimer.Interval = TarzanGame.TICK_MS
    End Sub

    ' ===================== UI CHON 1 NGUOI / HOST / JOIN =====================
    Private Sub BuildMenuUI()
        pnlMenu.Size = New Size(280, 210)
        pnlMenu.Location = New Point((Me.ClientSize.Width - pnlMenu.Width) \ 2, (Me.ClientSize.Height - pnlMenu.Height) \ 2)
        pnlMenu.BackColor = Color.FromArgb(230, 20, 40, 20)

        lblStatus.Text = "Chon kieu choi"
        lblStatus.ForeColor = Color.White
        lblStatus.AutoSize = True
        lblStatus.Location = New Point(20, 4)

        btnSolo.Text = "Choi 1 minh"
        btnSolo.Size = New Size(240, 36)
        btnSolo.Location = New Point(20, 30)
        AddHandler btnSolo.Click, AddressOf OnSoloClick

        btnHost.Text = "Choi 2 nguoi - Tao phong (Host)"
        btnHost.Size = New Size(240, 36)
        btnHost.Location = New Point(20, 74)
        AddHandler btnHost.Click, AddressOf OnHostClick

        txtIp.Text = "127.0.0.1"
        txtIp.Size = New Size(240, 24)
        txtIp.Location = New Point(20, 124)

        btnJoin.Text = "Choi 2 nguoi - Vao phong (Join)"
        btnJoin.Size = New Size(240, 36)
        btnJoin.Location = New Point(20, 154)
        AddHandler btnJoin.Click, AddressOf OnJoinClick

        pnlMenu.Controls.Add(lblStatus)
        pnlMenu.Controls.Add(btnSolo)
        pnlMenu.Controls.Add(btnHost)
        pnlMenu.Controls.Add(txtIp)
        pnlMenu.Controls.Add(btnJoin)
        Me.Controls.Add(pnlMenu)
    End Sub

    Private Sub OnSoloClick(sender As Object, e As EventArgs)
        isHost = True
        localPlayerIndex = 0
        isConnected = False
        game.IsSoloMode = True
        pnlMenu.Visible = False
        TickTimer.Start()
    End Sub

    Private Sub OnHostClick(sender As Object, e As EventArgs)
        isHost = True
        localPlayerIndex = 0
        game.IsSoloMode = False
        net = New NetworkPeer(Me)
        AddHandler net.LineReceived, AddressOf OnLineReceived
        AddHandler net.Connected, AddressOf OnPeerConnected
        AddHandler net.Disconnected, AddressOf OnPeerDisconnected
        net.StartHost(9898)
        lblStatus.Text = "Dang cho nguoi choi thu 2 ket noi... (port 9898)"
    End Sub

    Private Sub OnJoinClick(sender As Object, e As EventArgs)
        isHost = False
        localPlayerIndex = 1
        game.IsSoloMode = False
        net = New NetworkPeer(Me)
        AddHandler net.LineReceived, AddressOf OnLineReceived
        AddHandler net.Connected, AddressOf OnPeerConnected
        AddHandler net.Disconnected, AddressOf OnPeerDisconnected
        net.ConnectToHost(txtIp.Text.Trim(), 9898)
        lblStatus.Text = "Dang ket noi den " & txtIp.Text.Trim() & " ..."
    End Sub

    Private Sub OnPeerConnected()
        isConnected = True
        pnlMenu.Visible = False
        TickTimer.Start()
    End Sub

    Private Sub OnPeerDisconnected()
        isConnected = False
        TickTimer.Stop()
        pnlMenu.Visible = True
        lblStatus.Text = "Mat ket noi. Chon lai kieu choi."
    End Sub

    ' ===================== NHAN DU LIEU MANG =====================
    Private Sub OnLineReceived(line As String)
        If isHost Then
            If line.StartsWith("INPUT|") Then
                Dim inp As TarzanGame.PlayerInput = TarzanGame.ParseInput(line)
                game.SetInput(1, inp)
            End If
        Else
            If line.StartsWith("STATE|") Then
                game.ApplyStateLine(line)
            End If
        End If
    End Sub

    ' ===================== VONG LAP CHINH =====================
    Private Sub TickTimer_Tick(sender As Object, e As EventArgs) Handles TickTimer.Tick
        frameCounter += 1
        Dim localInp As New TarzanGame.PlayerInput()
        localInp.Left = keyLeft
        localInp.Right = keyRight
        localInp.Jump = keyJump
        localInp.Shoot = keyShoot

        If isHost Then
            game.SetInput(0, localInp)
            game.Tick()
            If isConnected Then
                net.SendLine(game.SerializeState())
            End If
        Else
            If isConnected Then
                net.SendLine(TarzanGame.SerializeInput(localInp))
            End If
        End If

        Me.Invalidate()
    End Sub

    ' ===================== BAT PHIM =====================
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        SetKeyState(e.KeyCode, True)
        MyBase.OnKeyDown(e)
    End Sub

    Protected Overrides Sub OnKeyUp(e As KeyEventArgs)
        SetKeyState(e.KeyCode, False)
        MyBase.OnKeyUp(e)
    End Sub

    Private Sub SetKeyState(key As Keys, isDown As Boolean)
        Select Case key
            Case Keys.Left, Keys.A : keyLeft = isDown
            Case Keys.Right, Keys.D : keyRight = isDown
            Case Keys.Up, Keys.W, Keys.Space, Keys.Z : keyJump = isDown ' cung la phim bam/tha day leo
            Case Keys.ControlKey, Keys.X : keyShoot = isDown
        End Select
    End Sub

    ' ===================== VE HINH (GDI+ / sprite fallback) =====================
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.InterpolationMode = InterpolationMode.NearestNeighbor
        g.Clear(Color.FromArgb(60, 110, 60))

        DrawBackground(g)
        DrawPlatforms(g)
        DrawVines(g)
        DrawPowerUps(g)
        DrawEnemies(g)
        DrawCoconuts(g)
        DrawPlayers(g)
        DrawHud(g)

        If Not isConnected AndAlso Not game.IsSoloMode Then
            Using f As New Font("Consolas", 10, FontStyle.Bold)
                g.DrawString("Chua ket noi - dung menu de Host/Join", f, Brushes.White, 10, TarzanGame.VIEW_HEIGHT_PX - 24)
            End Using
        End If

        MyBase.OnPaint(e)
    End Sub

    Private Function WorldToScreenX(worldX As Double) As Integer
        Return CInt(Math.Round(worldX - game.CameraX))
    End Function

    Private Sub DrawBackground(g As Graphics)
        If spBackground IsNot Nothing Then
            Dim offset As Integer = CInt(game.CameraX * 0.3) Mod spBackground.Width
            g.DrawImage(spBackground, -offset, 0)
            g.DrawImage(spBackground, -offset + spBackground.Width, 0)
        Else
            Using skyBrush As New SolidBrush(Color.FromArgb(60, 110, 60))
                g.FillRectangle(skyBrush, 0, 0, TarzanGame.VIEW_WIDTH_PX, TarzanGame.VIEW_HEIGHT_PX)
            End Using
            ' Vai lop tan la mo phia xa cho co chieu sau rung rai
            Using farBrush As New SolidBrush(Color.FromArgb(40, 85, 45))
                Dim offset As Integer = CInt(game.CameraX * 0.15) Mod 220
                Dim tx As Integer = -offset
                Do While tx < TarzanGame.VIEW_WIDTH_PX
                    g.FillEllipse(farBrush, tx, 30, 180, 90)
                    tx += 220
                Loop
            End Using
        End If
    End Sub

    ' Ve cac day leo tinh (o trang thai treo tu nhien) de nguoi choi biet cho nao can du qua.
    ' Neu co nguoi choi dang bam vao day nao thi ve theo dung goc lac hien tai cua nguoi do.
    Private Sub DrawVines(g As Graphics)
        For vi As Integer = 0 To game.Vines.Count - 1
            Dim v As TarzanGame.VineState = game.Vines(vi)
            Dim ax As Integer = WorldToScreenX(v.AnchorX)
            If ax < -60 OrElse ax > TarzanGame.VIEW_WIDTH_PX + 60 Then Continue For

            Dim tipX As Double = v.AnchorX
            Dim tipY As Double = v.AnchorY + v.Length

            For pi As Integer = 0 To game.ActivePlayerCount - 1
                Dim p As TarzanGame.PlayerState = game.Players(pi)
                If p.Alive AndAlso p.OnVine AndAlso p.VineIndex = vi Then
                    tipX = p.X + TarzanGame.PLAYER_W / 2.0
                    tipY = p.Y + TarzanGame.PLAYER_H / 2.0
                    Exit For
                End If
            Next

            Dim tipSx As Integer = WorldToScreenX(tipX)
            Using vinePen As New Pen(Color.FromArgb(255, 90, 60, 25), 4)
                g.DrawLine(vinePen, ax, CInt(v.AnchorY), tipSx, CInt(tipY))
            End Using
            Using leafBrush As New SolidBrush(Color.FromArgb(255, 60, 140, 50))
                g.FillEllipse(leafBrush, ax - 6, CInt(v.AnchorY) - 6, 12, 12)
            End Using
        Next
    End Sub

    Private Sub DrawPlatforms(g As Graphics)
        For Each plat In game.Platforms
            Dim sx As Integer = WorldToScreenX(plat.X)
            If sx + plat.W < 0 OrElse sx > TarzanGame.VIEW_WIDTH_PX Then Continue For
            Dim sy As Integer = CInt(plat.Y)
            Dim w As Integer = CInt(plat.W)
            Dim h As Integer = CInt(plat.H)

            Select Case plat.Kind
                Case TarzanGame.PlatformKind.Ground
                    If spGround IsNot Nothing Then
                        Dim tx As Integer = sx
                        Do While tx < sx + w
                            g.DrawImage(spGround, tx, sy)
                            tx += spGround.Width
                        Loop
                    Else
                        Using b As New SolidBrush(Color.FromArgb(110, 75, 40))
                            g.FillRectangle(b, sx, sy, w, h)
                        End Using
                        Using topB As New SolidBrush(Color.FromArgb(60, 170, 60))
                            g.FillRectangle(topB, sx, sy, w, 8)
                        End Using
                    End If

                Case TarzanGame.PlatformKind.OneWay
                    Using b As New SolidBrush(Color.FromArgb(90, 60, 35))
                        g.FillRectangle(b, sx, sy, w, h)
                    End Using
                    Using topB As New SolidBrush(Color.FromArgb(50, 150, 55))
                        g.FillRectangle(topB, sx, sy, w, 5)
                    End Using
                    g.DrawRectangle(Pens.Black, sx, sy, w, h)

                Case TarzanGame.PlatformKind.HollowStump
                    If spHollowStump IsNot Nothing Then
                        g.DrawImage(spHollowStump, sx, sy, w, h)
                    Else
                        Using b As New SolidBrush(Color.FromArgb(80, 55, 30))
                            g.FillRectangle(b, sx, sy, w, h)
                        End Using
                        Using rim As New SolidBrush(Color.FromArgb(45, 30, 15))
                            g.FillRectangle(rim, sx - 4, sy, w + 8, 14)
                        End Using
                        g.DrawRectangle(Pens.Black, sx, sy, w, h)
                    End If

                Case TarzanGame.PlatformKind.LogPile
                    Using b As New SolidBrush(Color.FromArgb(120, 80, 45))
                        g.FillRectangle(b, sx, sy, w, h)
                    End Using
                    g.DrawRectangle(Pens.Black, sx, sy, w, h)
                    g.DrawLine(Pens.Black, sx + w \ 2, sy, sx + w \ 2, sy + h)

                Case TarzanGame.PlatformKind.MysteryCrate
                    If plat.Used Then
                        Using b As New SolidBrush(Color.FromArgb(70, 55, 35))
                            g.FillRectangle(b, sx, sy, w, h)
                        End Using
                        g.DrawRectangle(Pens.Black, sx, sy, w, h)
                    ElseIf spMysteryCrate IsNot Nothing Then
                        g.DrawImage(spMysteryCrate, sx, sy, w, h)
                    Else
                        Using b As New SolidBrush(Color.FromArgb(200, 150, 70))
                            g.FillRectangle(b, sx, sy, w, h)
                        End Using
                        g.DrawRectangle(Pens.Black, sx, sy, w, h)
                        Using f As New Font("Consolas", 12, FontStyle.Bold)
                            g.DrawString("?", f, Brushes.White, sx + w / 4.0F, sy - 1)
                        End Using
                    End If
            End Select
        Next
    End Sub

    Private Sub DrawPlayers(g As Graphics)
        For i As Integer = 0 To game.ActivePlayerCount - 1
            Dim p As TarzanGame.PlayerState = game.Players(i)
            If Not p.Alive Then Continue For

            Dim sx As Integer = WorldToScreenX(p.X)
            Dim sy As Integer = CInt(p.Y)
            Dim blink As Boolean = (p.InvulnTicks > 0) AndAlso ((p.InvulnTicks \ 4) Mod 2 = 0)
            If blink Then Continue For

            Dim sheet As SpriteSheet = If(i = 0, sheetPlayer0, sheetPlayer1)
            If sheet.Sheet IsNot Nothing Then
                Dim frameIdx As Integer = ChooseSheetFrame(sheet, p.OnGround OrElse p.OnVine, p.IsMoving)
                DrawSheetFrame(g, sheet, frameIdx, sx, sy, TarzanGame.PLAYER_W, TarzanGame.PLAYER_H, Not p.FacingRight)
                DrawPowerBadge(g, p, sx, sy)
                Continue For
            End If

            Dim baseSprite As Bitmap = If(i = 0, spPlayer0, spPlayer1)
            Dim walk2Sprite As Bitmap = If(i = 0, spPlayer0Walk2, spPlayer1Walk2)
            Dim jumpSprite As Bitmap = If(i = 0, spPlayer0Jump, spPlayer1Jump)

            Dim sprite As Bitmap
            If (Not p.OnGround OrElse p.OnVine) AndAlso jumpSprite IsNot Nothing Then
                sprite = jumpSprite
            ElseIf p.IsMoving AndAlso p.OnGround AndAlso walk2Sprite IsNot Nothing AndAlso ((frameCounter \ 6) Mod 2 = 1) Then
                sprite = walk2Sprite
            Else
                sprite = baseSprite
            End If

            If sprite IsNot Nothing Then
                Dim st As GraphicsState = g.Save()
                If Not p.FacingRight Then
                    g.TranslateTransform(sx + TarzanGame.PLAYER_W, sy)
                    g.ScaleTransform(-1, 1)
                    g.DrawImage(sprite, 0, 0, TarzanGame.PLAYER_W, TarzanGame.PLAYER_H)
                Else
                    g.DrawImage(sprite, sx, sy, TarzanGame.PLAYER_W, TarzanGame.PLAYER_H)
                End If
                g.Restore(st)
            Else
                ' Mau theo cap do suc manh: xam nhat = yeu, xanh la = khoe (an chuoi), cam = nem duoc dua
                Dim c As Color = If(p.WeaponLevel >= 2, Color.SaddleBrown, If(p.WeaponLevel = 1, If(i = 0, Color.LightGreen, Color.LightSkyBlue), Color.Gray))
                Using b As New SolidBrush(c)
                    g.FillRectangle(b, sx, sy, TarzanGame.PLAYER_W, TarzanGame.PLAYER_H)
                End Using
                g.DrawRectangle(Pens.Black, sx, sy, TarzanGame.PLAYER_W, TarzanGame.PLAYER_H)
                Dim cx As Integer = If(p.FacingRight, sx + TarzanGame.PLAYER_W - 4, sx)
                g.FillEllipse(Brushes.White, cx - 2, sy + 6, 6, 6)
            End If
            DrawPowerBadge(g, p, sx, sy)
        Next
    End Sub

    Private Sub DrawPowerBadge(g As Graphics, p As TarzanGame.PlayerState, sx As Integer, sy As Integer)
        If p.WeaponLevel >= 2 Then
            Using f As New Font("Consolas", 8, FontStyle.Bold)
                g.DrawString("*", f, Brushes.SaddleBrown, sx + 6, sy - 12)
            End Using
        End If
    End Sub

    Private Sub DrawEnemies(g As Graphics)
        For Each en In game.Enemies
            If Not en.Alive Then Continue For
            Dim sx As Integer = WorldToScreenX(en.X)
            If sx < -60 OrElse sx > TarzanGame.VIEW_WIDTH_PX + 60 Then Continue For
            Dim sy As Integer = CInt(en.Y)

            Dim sprite As Bitmap = Nothing
            Dim fallbackColor As Color = Color.SaddleBrown
            Dim w As Integer = TarzanGame.ENEMY_SMALL_W
            Dim h As Integer = TarzanGame.ENEMY_SMALL_H
            Dim useWalk2 As Boolean = ((frameCounter \ 6) Mod 2 = 1)

            Select Case en.Kind
                Case TarzanGame.EnemyType.Leopard
                    sprite = If(useWalk2 AndAlso spLeopardWalk2 IsNot Nothing, spLeopardWalk2, spLeopard)
                    fallbackColor = Color.Goldenrod

                Case TarzanGame.EnemyType.Pangolin
                    fallbackColor = If(en.Curled, Color.DarkGreen, Color.ForestGreen)
                    sprite = Nothing ' luon fallback GDI+ de the hien ro trang thai di/cuon banh

                Case TarzanGame.EnemyType.Gorilla
                    sprite = If(useWalk2 AndAlso spGorillaWalk2 IsNot Nothing, spGorillaWalk2, spGorilla)
                    fallbackColor = Color.DimGray : w = TarzanGame.ENEMY_BOSS_W : h = TarzanGame.ENEMY_BOSS_H
            End Select

            If en.Kind = TarzanGame.EnemyType.Pangolin Then
                DrawPangolin(g, en, sx, sy, w, h)
                Continue For
            End If

            Dim sheet As SpriteSheet = Nothing
            If en.Kind = TarzanGame.EnemyType.Leopard Then
                sheet = sheetLeopard
            ElseIf en.Kind = TarzanGame.EnemyType.Gorilla Then
                sheet = sheetGorilla
            End If

            If sheet.Sheet IsNot Nothing Then
                Dim frameIdx As Integer = ChooseSheetFrame(sheet, True, True)
                DrawSheetFrame(g, sheet, frameIdx, sx, sy, w, h, Not en.FacingRight)
            ElseIf sprite IsNot Nothing Then
                If Not en.FacingRight Then
                    Dim st As GraphicsState = g.Save()
                    g.TranslateTransform(sx + w, sy)
                    g.ScaleTransform(-1, 1)
                    g.DrawImage(sprite, 0, 0, w, h)
                    g.Restore(st)
                Else
                    g.DrawImage(sprite, sx, sy, w, h)
                End If
            Else
                Using b As New SolidBrush(fallbackColor)
                    g.FillRectangle(b, sx, sy, w, h)
                End Using
                g.DrawRectangle(Pens.Black, sx, sy, w, h)
            End If

            If en.Kind = TarzanGame.EnemyType.Gorilla Then
                Dim barW As Integer = TarzanGame.ENEMY_BOSS_W
                g.DrawRectangle(Pens.White, sx, sy - 10, barW, 6)
                Dim hpRatio As Double = Math.Max(0.0, Math.Min(1.0, en.HP / 6.0))
                Using hb As New SolidBrush(Color.Red)
                    g.FillRectangle(hb, sx, sy - 10, CInt(barW * hpRatio), 6)
                End Using
            End If
        Next
    End Sub

    ' Te te (Pangolin): dang di (fallback hinh oval xanh nhat) -> bi dam thanh banh cuon tron
    ' (fallback bet mau xanh dam, vien do khi dang bi da lan de canh bao nguy hiem).
    Private Sub DrawPangolin(g As Graphics, en As TarzanGame.EnemyState, sx As Integer, sy As Integer, w As Integer, h As Integer)
        If Not en.Curled Then
            If spPangolin IsNot Nothing Then
                If Not en.FacingRight Then
                    Dim st As GraphicsState = g.Save()
                    g.TranslateTransform(sx + w, sy)
                    g.ScaleTransform(-1, 1)
                    g.DrawImage(spPangolin, 0, 0, w, h)
                    g.Restore(st)
                Else
                    g.DrawImage(spPangolin, sx, sy, w, h)
                End If
            Else
                Using b As New SolidBrush(Color.ForestGreen)
                    g.FillEllipse(b, sx, sy, w, h)
                End Using
                g.DrawEllipse(Pens.Black, sx, sy, w, h)
            End If
        Else
            Dim ballSprite As Bitmap = If(en.RollingBall AndAlso spPangolinBallRoll IsNot Nothing, spPangolinBallRoll, spPangolinBall)
            If ballSprite IsNot Nothing Then
                ' Dung dung ti le anh that (khong ep vuong/det) de banh tron khong bi bop meo.
                Dim aspect As Double = ballSprite.Width / CDbl(ballSprite.Height)
                Dim drawH As Integer = h
                Dim drawW As Integer = CInt(drawH * aspect)
                Dim movingRight As Boolean = (en.VelX >= 0)
                Dim boxY As Integer = sy + (h - drawH)

                If Not en.RollingBall Then
                    ' Banh dung yen: can giua trong khung, khong can lat huong
                    Dim boxX As Integer = sx + (w - drawW) \ 2
                    g.DrawImage(ballSprite, boxX, boxY, drawW, drawH)
                Else
                    ' Banh dang lan: anh nguon co ve mo phia sau (gia dinh lan sang phai).
                    ' Lan phai -> giu nguyen, can sat canh phai khung (dau banh o phia truoc).
                    ' Lan trai -> lat ngang + can sat canh trai khung.
                    Dim boxX As Integer = If(movingRight, sx + w - drawW, sx)
                    If movingRight Then
                        g.DrawImage(ballSprite, boxX, boxY, drawW, drawH)
                    Else
                        Dim st As GraphicsState = g.Save()
                        g.TranslateTransform(boxX + drawW, boxY)
                        g.ScaleTransform(-1, 1)
                        g.DrawImage(ballSprite, 0, 0, drawW, drawH)
                        g.Restore(st)
                    End If
                    g.DrawRectangle(Pens.Red, sx, sy, w, h)
                End If
            Else
                Dim ballH As Integer = h \ 2
                Dim ballY As Integer = sy + (h - ballH)
                Using b As New SolidBrush(Color.DarkGreen)
                    g.FillEllipse(b, sx, ballY, w, ballH)
                End Using
                Dim pen As Pen = If(en.RollingBall, Pens.Red, Pens.Black)
                g.DrawEllipse(pen, sx, ballY, w, ballH)
            End If
        End If
    End Sub

    Private Sub DrawCoconuts(g As Graphics)
        For Each cn In game.Coconuts
            If Not cn.Active Then Continue For
            Dim sx As Integer = WorldToScreenX(cn.X)
            If sx < -20 OrElse sx > TarzanGame.VIEW_WIDTH_PX + 20 Then Continue For
            Dim sy As Integer = CInt(cn.Y)

            Dim sprite As Bitmap = If(cn.Owner >= 0, spCoconutPlayer, spCoconutEnemy)
            If sprite IsNot Nothing Then
                g.DrawImage(sprite, sx - 8, sy - 8, 16, 16)
            Else
                Dim c As Color = If(cn.Owner >= 0, Color.SaddleBrown, Color.SandyBrown)
                Using b As New SolidBrush(c)
                    g.FillEllipse(b, sx - 8, sy - 8, 16, 16)
                End Using
                g.DrawEllipse(Pens.Black, sx - 8, sy - 8, 16, 16)
            End If
        Next
    End Sub

    Private Sub DrawPowerUps(g As Graphics)
        For Each pu In game.PowerUps
            If Not pu.Active Then Continue For
            Dim sx As Integer = WorldToScreenX(pu.X)
            If sx < -40 OrElse sx > TarzanGame.VIEW_WIDTH_PX + 40 Then Continue For
            Dim sy As Integer = CInt(pu.Y)

            Select Case pu.Kind
                Case TarzanGame.PowerUpType.Fruit
                    If spPowerFruit IsNot Nothing Then
                        g.DrawImage(spPowerFruit, sx, sy, 24, 24)
                    Else
                        Using b As New SolidBrush(Color.OrangeRed)
                            g.FillEllipse(b, sx, sy, 24, 24)
                        End Using
                        g.DrawEllipse(Pens.DarkRed, sx, sy, 24, 24)
                    End If
                Case TarzanGame.PowerUpType.Banana
                    DrawPowerUpSprite(g, spPowerBanana, Color.Gold, sx, sy)
                Case TarzanGame.PowerUpType.MagicCoconut
                    DrawPowerUpSprite(g, spPowerMagicCoconut, Color.SaddleBrown, sx, sy)
                Case TarzanGame.PowerUpType.HeartOfJungle
                    DrawPowerUpSprite(g, spPowerHeart, Color.Crimson, sx, sy)
            End Select
        Next
    End Sub

    Private Sub DrawPowerUpSprite(g As Graphics, sprite As Bitmap, fallbackColor As Color, sx As Integer, sy As Integer)
        If sprite IsNot Nothing Then
            g.DrawImage(sprite, sx, sy, 30, 30)
        Else
            Using b As New SolidBrush(fallbackColor)
                g.FillEllipse(b, sx, sy, 30, 30)
            End Using
            g.DrawEllipse(Pens.Black, sx, sy, 30, 30)
        End If
    End Sub

    Private Sub DrawHud(g As Graphics)
        Using f As New Font("Consolas", 11, FontStyle.Bold)
            Dim txt As String
            If game.IsSoloMode Then
                txt = String.Format("Mang: {0}   Trai cay: {1}   Diem: {2}   Lv{3}",
                    game.SharedLives, game.SharedFruits, game.SharedScore, game.Players(0).WeaponLevel)
            Else
                txt = String.Format("Mang: {0}   Trai cay: {1}   Diem: {2}   P1 Lv{3}   P2 Lv{4}",
                    game.SharedLives, game.SharedFruits, game.SharedScore, game.Players(0).WeaponLevel, game.Players(1).WeaponLevel)
            End If
            g.DrawString(txt, f, Brushes.White, 8, 6)

            If game.GameOver Then
                DrawCenteredBanner(g, "GAME OVER", Color.Red)
            ElseIf game.Victory Then
                DrawCenteredBanner(g, "CHIEN THANG!", Color.Gold)
            End If
        End Using
    End Sub

    Private Sub DrawCenteredBanner(g As Graphics, text As String, c As Color)
        Using f As New Font("Consolas", 28, FontStyle.Bold)
            Dim sz As SizeF = g.MeasureString(text, f)
            Dim x As Single = (TarzanGame.VIEW_WIDTH_PX - sz.Width) / 2.0F
            Dim y As Single = (TarzanGame.VIEW_HEIGHT_PX - sz.Height) / 2.0F
            Using b As New SolidBrush(c)
                g.DrawString(text, f, b, x, y)
            End Using
        End Using
    End Sub

    ' ===================== NAP SPRITE (tuy chon) =====================
    ' Tai su dung dung ten file PNG da co san trong thu muc Assets (khong bat buoc doi ten):
    ' neu khong tim thay file, tu dong fallback ve bang GDI+.
    Private Sub LoadSpritesIfExist()
        Dim dir As String = AppDomain.CurrentDomain.BaseDirectory
        Dim assetsDir As String = Path.Combine(dir, "Assets")

        sheetPlayer0 = TryLoadSheet(assetsDir, "player0_sheet.png", TarzanGame.PLAYER_W, TarzanGame.PLAYER_H)
        sheetPlayer1 = TryLoadSheet(assetsDir, "player1_sheet.png", TarzanGame.PLAYER_W, TarzanGame.PLAYER_H)
        sheetLeopard = TryLoadSheet(assetsDir, "enemy_walker_sheet.png", 28, 32)
        sheetGorilla = TryLoadSheet(assetsDir, "enemy_boss_sheet.png", 64, 64)

        spPlayer0 = TryLoad(assetsDir, "player0.png")
        spPlayer0Walk2 = TryLoad(assetsDir, "player0_walk2.png")
        spPlayer0Jump = TryLoad(assetsDir, "player0_jump.png")
        spPlayer1 = TryLoad(assetsDir, "player1.png")
        spPlayer1Walk2 = TryLoad(assetsDir, "player1_walk2.png")
        spPlayer1Jump = TryLoad(assetsDir, "player1_jump.png")
        spLeopard = TryLoad(assetsDir, "enemy_soldier.png")
        spLeopardWalk2 = TryLoad(assetsDir, "enemy_soldier_walk2.png")
        spPangolin = TryLoad(assetsDir, "enemy_shelled.png")
        spPangolinBall = TryLoad(assetsDir, "enemy_shell.png")
        spPangolinBallRoll = TryLoad(assetsDir, "enemy_shell_roll.png")
        spGorilla = TryLoad(assetsDir, "enemy_boss.png")
        spGorillaWalk2 = TryLoad(assetsDir, "enemy_boss_walk2.png")
        spGround = TryLoad(assetsDir, "tile_ground.png")
        spMysteryCrate = TryLoad(assetsDir, "tile_questionblock.png")
        spHollowStump = TryLoad(assetsDir, "tile_pipe.png")
        spCoconutPlayer = TryLoad(assetsDir, "bullet_player.png")
        spCoconutEnemy = TryLoad(assetsDir, "bullet_enemy.png")
        spPowerBanana = TryLoad(assetsDir, "powerup_banana.png")
        spPowerMagicCoconut = TryLoad(assetsDir, "powerup_coconut.png")
        spPowerHeart = TryLoad(assetsDir, "powerup_life.png")
        spPowerFruit = TryLoad(assetsDir, "powerup_fruit.png")
        spBackground = TryLoad(assetsDir, "background.png")
    End Sub

    Private Function TryLoadSheet(assetsDir As String, fileName As String, frameW As Integer, frameH As Integer) As SpriteSheet
        Dim result As New SpriteSheet()
        Dim bmp As Bitmap = TryLoad(assetsDir, fileName)
        If bmp Is Nothing Then Return result
        result.Sheet = bmp
        result.FrameW = frameW
        result.FrameH = frameH
        result.FrameCount = Math.Max(1, bmp.Width \ frameW)
        Return result
    End Function

    Private Function ChooseSheetFrame(sheet As SpriteSheet, onGround As Boolean, isMoving As Boolean) As Integer
        Dim idleIdx As Integer = 0
        Dim walk1Idx As Integer = Math.Min(1, sheet.FrameCount - 1)
        Dim walk2Idx As Integer = Math.Min(2, sheet.FrameCount - 1)
        Dim jumpIdx As Integer = Math.Min(3, sheet.FrameCount - 1)

        If Not onGround Then Return jumpIdx
        If isMoving Then Return If((frameCounter \ 6) Mod 2 = 0, walk1Idx, walk2Idx)
        Return idleIdx
    End Function

    Private Sub DrawSheetFrame(g As Graphics, sheet As SpriteSheet, frameIdx As Integer, destX As Integer, destY As Integer, drawW As Integer, drawH As Integer, flipLeft As Boolean)
        Dim clamped As Integer = Math.Max(0, Math.Min(frameIdx, sheet.FrameCount - 1))
        Dim srcRect As New Rectangle(clamped * sheet.FrameW, 0, sheet.FrameW, sheet.FrameH)

        If flipLeft Then
            Dim st As GraphicsState = g.Save()
            g.TranslateTransform(destX + drawW, destY)
            g.ScaleTransform(-1, 1)
            g.DrawImage(sheet.Sheet, New Rectangle(0, 0, drawW, drawH), srcRect, GraphicsUnit.Pixel)
            g.Restore(st)
        Else
            g.DrawImage(sheet.Sheet, New Rectangle(destX, destY, drawW, drawH), srcRect, GraphicsUnit.Pixel)
        End If
    End Sub

    Private Function TryLoad(assetsDir As String, fileName As String) As Bitmap
        Try
            Dim fullPath As String = Path.Combine(assetsDir, fileName)
            If File.Exists(fullPath) Then
                Return New Bitmap(fullPath)
            End If
        Catch ex As Exception
            ' Loi doc file: bo qua, dung fallback GDI+
        End Try
        Return Nothing
    End Function

End Class
