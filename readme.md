# 電光掲示板制御プログラム
## これなに
電光掲示板を制御するためのプログラムです。   

## License
このプログラムには  
MicroTimer(CodeProjectOpenLicense) : http://www.codeproject.com/Articles/98346/Microsecond-and-Millisecond-NET-Timer  
CircularBuffer(ApacheLicense) : https://github.com/ufcpp/UfcppSample/blob/master/Chapters/Algorithm/Collections/CircularBuffer.cs  
DirectShowLib(LGPLv2) :
http://directshownet.sourceforge.net/  
を使用しております。  
CodeProjectOpenLicenseに関してはCPOL.htmlを参照してください。  
ApacheLicense,LGPLv2はLICENSEを参照してください。  
このプログラム自体はMITライセンスです。  
ただコレをどこで使ったよって@surigomaxxxxxxxとかに投げたり、コレ使ったよってこのページのリンクを書いてくれると僕が死ぬほど喜びます。

## 機能
* 文字スクロール
* 動画再生(AVI,WMVのみしか確認してない)
* ペイント機能
* ボリュームメータ
* FFT表示
* 波形表示
* ビート検出(未実装)

## 技術的なこと
**回路は適当に作ってください。**  
残念ながら公開することを検討してなかったので、**コメントは一切含まれていません。**  
データ方式はDataProtocol.mdを参照してください。   
画像処理は2値化処理を行っております。  
2値化処理は以下の手法を導入しております。
* 単純２値化 固定値(明度, Rのみ, Gのみ, Bのみ)
* 単純２値化 可変値(全体平均)
* 単純２値化 固定値 + エッジ処理
* ディザ法 (Bayer, ハーフトーン, 2px*2px)
* 誤差分散法(Sierra Lite)

## 使用ライブラリ
### Nugetから
* NAudio
* MathNet

### Webから
* DirectShowLib(LGPLv2) : http://directshownet.sourceforge.net/
* MicroTimer(CPOL) : http://www.codeproject.com/Articles/98346/Microsecond-and-Millisecond-NET-Timer  
* CircularBuffer(ApacheLicense) : https://github.com/ufcpp/UfcppSample/blob/master/Chapters/Algorithm/Collections/CircularBuffer.cs
