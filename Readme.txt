■できること
舞台は、迷路！、ステージ上には、至る所にコインが配置されています。
あなたは、アバターとして、ユニティちゃんを操作します。
複数のユーザーが、同時にネットワーク接続し、ボイスチャットしながら、
わいわい遊べるアプリケーションとなっています。
迷路は自動生成で、日毎に変わるようにしています。

https://youtu.be/jO5EM-g4sTU

■開発環境
-------------------------------------------------------------------
サービス	種類	備考
-------------------------------------------------------------------
TencentCloud	Voice Chat	GME Voice Chat 2022-04-12
Unity	UNITY 2020.3.42f1	Windowsスタンドアロンアプリケーション
Unity Asset	ゲームに使用	Maze Generator
MLAPI Sample	ゲームに使用	MLAPI_UnitychanSample
-------------------------------------------------------------------


■ボイスチャットについて
TencentCloud の GME「Game Multimedia Engine」を使用しています。
アカウントを作成し、スクリプトGmeVoiceChatScript.cs の
「sdkAppId」に、AppIDを、「authkey」に、Permission key を設定してください。
