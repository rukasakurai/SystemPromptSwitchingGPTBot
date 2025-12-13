# Teams マニフェストファイルについて

このディレクトリには Teams アプリのマニフェストファイルとアイコンが含まれています。

## ファイル構成

- `manifest.json` - 実際に使用されているマニフェストファイル
- `manifest.json.template` - テンプレートファイル（新規セットアップ時の参考用）
- `color.png` - カラーアイコン（192x192 ピクセル）
- `outline.png` - アウトラインアイコン（32x32 ピクセル）

## マニフェストファイルの重要な設定項目

マニフェストファイルには、Azure Bot Service の **Microsoft App ID** を設定する必要がある箇所が 3 つあります：

### 1. アプリケーション ID
```json
"id": "ここに Microsoft App ID を設定"
```

### 2. ボット ID
```json
"bots": [
    {
        "botId": "ここに Microsoft App ID を設定",
        ...
    }
]
```

### 3. Copilot エージェント ID（Copilot 統合を使用する場合）
```json
"copilotAgents": {
    "customEngineAgents": [
        {
            "id": "ここに Microsoft App ID を設定",
            ...
        }
    ]
}
```

## 新規セットアップまたは Azure Bot を再作成した場合

Azure Bot Service を新規作成した場合、または既存のものを削除して再作成した場合は、以下の手順でマニフェストファイルを更新してください：

1. Azure Bot の「構成」画面から **Microsoft App ID** をコピーします
2. `manifest.json` を開きます
3. 上記 3 箇所の Microsoft App ID をすべて新しい値に更新します
4. `manifest.json`、`color.png`、`outline.png` の 3 ファイルを ZIP 圧縮します
   - **重要**：ファイルを直接選択して圧縮してください（フォルダごと圧縮しないこと）
5. Teams で新しい ZIP ファイルをアップロードします

## トラブルシューティング

### マニフェストのアップロードに失敗する

- 3 つのファイルが直接 ZIP のルートにあることを確認してください（フォルダ内にないこと）
- `manifest.json` の JSON 構文が正しいことを確認してください
- すべての `<xxx>` プレースホルダーが実際の値に置き換わっていることを確認してください

### ボットが Teams で応答しない

1. `manifest.json` の `id`、`botId`、`copilotAgents.customEngineAgents[].id` がすべて同じ Microsoft App ID であることを確認してください
2. Azure Web Apps の「アプリケーション設定」の `MicrosoftAppId` と一致していることを確認してください
3. 詳細は[メインの README のトラブルシューティングセクション](../README.md#トラブルシューティング)を参照してください
