# Tarzan Rung Xanh - Co-op Online

Chuyen the tu du an platformer "Phieu Luu Ong Nuoc" (PlatformGame.vb) sang chu de rung
ram kieu Tarzan, giu nguyen kien truc mang Host-authoritative / Client chi gui INPUT qua
NetworkPeer (TCP, pipe-delimited), 2 nguoi choi co-op vuot man.

## Diem moi so voi ban goc

- **Du day leo (Vine)**: co 3 khe vuc/song bat buoc tren duong di (quanh x=1400-1500,
  2600-2750, 4200-4350). Khong co nen dat o do - roi xuong se mat mang. Nhay len gan
  day leo va bam lai phim Nhay (Space/Up/W/Z) de bam vao, giu Trai/Phai de nhun tang da,
  bam Nhay lan nua de tha day va bay ve huong dang lac toi. Da (momentum) luc tha duoc
  giu lai va giam dan, giup bay xa hon neu tha dung luc.
- **Quai vat doi ten/vai tro**: Leopard (bao rinh di tuan) thay Walker, Pangolin (te te,
  dam dau lan dau se cuon tron thanh banh, da banh de diet quai khac) thay Shelled,
  Gorilla (khi dot dau dan, quang dua) thay Boss.
- **Vat pham doi ten**: Banana (chuoi, +1 cap suc manh), MagicCoconut (dua than, mo khoa
  nem dua tam xa), HeartOfJungle (+1 mang), Fruit (trai cay, +diem/tien te chung, du
  FRUITS_PER_LIFE trai se +1 mang).
- Vu khi tam xa doi thanh **nem dua** (Coconut) - ca nguoi choi va Gorilla deu dung chung
  co che nay.
- HUD/tieu de/nhan chu doi sang chu de rung.

## Asset hien co / con thieu

Da co (trong `Assets/`, dung ten, dung kich thuoc code can, nen trong suot):

- Nhan vat: `player0.png/_walk2/_jump`, `player1.png/_walk2/_jump` (24x32)
- Quai: `enemy_soldier.png/_walk2` (Leopard, 28x32), `enemy_boss.png/_walk2` (Gorilla, 64x64),
  `enemy_shelled.png` (Pangolin dang di, 28x32)
- Vat pham: `powerup_banana.png`, `powerup_coconut.png`, `powerup_life.png`,
  `powerup_fruit.png` (20x20 / 16x16), `bullet_player.png`, `bullet_enemy.png` (10x10)
- Dia hinh: `tile_questionblock.png` (32x32), `tile_pipe.png` (goc cay rong, ty le tu do,
  code tu keo gian theo kich thuoc tung platform), `background.png` (860x480)

Con thieu (dang fallback ve bang GDI+, KHONG loi, chi la chua co hinh that):

- `tile_ground.png` - nen dat rung (hien fallback: khoi nau + vien xanh la tren dinh)
- `enemy_shell.png` - Pangolin luc cuon tron thanh banh (hien fallback: hinh oval xanh
  dam, vien do khi dang lan nguy hiem)

Neu ve them 2 file nay, chi can dat dung ten vao `Assets/` la game tu dong dung, khong
can sua code.

## Kieu choi

Menu luc mo game co 3 lua chon:
- **Choi 1 minh**: chay ngay tren may, khong can mang, chi dieu khien 1 nhan vat (Player 1).
  Dieu kien thua/thang chi tinh tren nguoi choi nay.
- **Choi 2 nguoi - Tao phong (Host)**: mo cong 9898 cho nguoi thu 2 Join vao.
- **Choi 2 nguoi - Vao phong (Join)**: nhap IP cua Host de ket noi.

## Man hinh & kich thuoc (cap nhat)

- Do phan giai: **960x540** (truoc la 800x480).
- Nhan vat: 36x48px (truoc 24x32). Quai nho (Leopard/Pangolin): 42x48px (truoc 28x32).
  Trum Gorilla: 96x96px (truoc 64x64). Vat pham/dua nem cung tang ty le tuong ung.
- Vat ly (toc do di chuyen, luc nhay, trong luc...) da tang ty le ~1.3x cho khop voi
  nhan vat to hon, giu cam giac choi tuong tu ban truoc, khong bi cham/nang hon.
- Cac file sprite nhan vat/quai hien co van dung duoc binh thuong (game tu keo gian
  NEAREST-neighbor len kich thuoc moi, giu net pixel art khong bi mo). Neu muon net
  net hon o kich thuoc lon, co the ve lai o do phan giai cao hon sau.

## Giu nguyen tu ban goc

- Vat ly nhay/roi, va cham AABB voi platform (Ground/OneWay/HollowStump/LogPile), khoi
  bi an dam dau tu duoi len (MysteryCrate, truoc la khoi "?").
- Dam dau (stomp) tieu diet quai, he thong mang chung (SharedLives), diem, respawn co
  bat tu (invuln ticks).
- Giao thuc mang STATE|... / INPUT|... qua NetworkHub/NetworkPeer, Host tinh toan
  authoritative, Client chi gui input va nhan state de ve.
- Sprite fallback GDI+ khi thieu file PNG trong Assets/ - tai su dung dung ten file cu
  (player0.png, enemy_soldier.png, enemy_shelled.png, enemy_shell.png, enemy_boss.png,
  tile_ground.png, tile_pipe.png, tile_questionblock.png, bullet_player.png,
  bullet_enemy.png, powerup_banana.png, powerup_coconut.png, powerup_life.png,
  powerup_fruit.png, background.png) de khong phai
  doi ten file anh da co san. Rieng day leo luon ve bang GDI+ (khong can sprite rieng).

## Build

Chay `build_tarzan.bat` (can .NET Framework 4.x SDK / vbc.exe co san tren may, giong
cach build cac du an VB.NET khac). File thuc thi la `TarzanRungXanhCoop.exe`, dat cung
cap voi thu muc `Assets/`.

## Dieu khien

- Trai/Phai (hoac A/D): di chuyen / nhun day khi dang du
- Nhay (Space/Up/W/Z): nhay thuong, hoac bam/tha day leo khi o gan/dang tren day
- Ctrl/X: nem dua (can WeaponLevel >= 2, tuc da an Trai Dua Than)

## File

- `TarzanGame.vb` - toan bo logic game (vat ly, quai, vat pham, du day leo, giao thuc mang)
- `Form1.vb` - UI menu Host/Join, ve hinh (GDI+), bat phim, vong lap Timer
- `NetworkPeer.vb` - khong doi so voi ban goc
- `Program.vb` - diem vao chuong trinh, khong doi
- `build_tarzan.bat` - script build bang vbc.exe (.NET Framework 4.x)
- `Assets/` - sprite PNG tai su dung tu ban goc (fallback GDI+ neu thieu)
