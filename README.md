# SystemPromptSwitchingGPTBot

１つのボットでシステムプロンプトや各種パラメーターを切り替えながら ChatGPT と対話できます。構想としては GPTs を１つのボットの中で使うようなイメージです。
Teams を前提に開発していますが、Azure Bot を使用しているため Slack や LINE などの各種チャットサービスにも対応しているはずです。
![](./assets/character.gif)
![](./assets/arch.png)

## セットアップ方法

### 前提条件

- このリポジトリをフォーク&クローンまたはダウンロード
- Azure OpenAI Service または OpenAI の準備

  - Azure OpenAI Service：GPT-35-turbo または GPT-4 のデプロイ

- ローカル実行を行う場合は以下の環境がローカルマシンに準備されていること
  - .NET 8
  - VS Code または Visual Studio
  - Bot Framework Emulator
  - Azure CLI

### Azure OpenAI Service のデプロイ

1. Azure OpenAI Service をデプロイします。
1. gpt-35-turbo または gpt-4 のいずれかをデプロイします。

### システムプロンプト設定の改変

[app/GptConfiguration/](https://github.com/07JP27/SystemPromptSwitchingGPTBot/tree/main/app/GptConfiguration)ディレクトリ配下に各種システムプロンプトの設定ファイルがあります。これらのファイルを編集することでシステムプロンプトの設定を変更できます。また、`IGptConfiguration`を実装したクラスを作成することで独自のシステムプロンプト設定を新規作成することもできます。
![](./assets/classmap.png)

### ローカル実行

1. 対象の Azure OpenAI Service のアクセス制御でローカル実行ユーザーに RBAC「Cognitive Services OpenAI User」ロールを付与します。**すでに共同作成者がついている場合でも必ず別途付与してください**
1. `appsettings.Development.json`に Azure OpenAI Service のエンドポイントとデプロイ名をセットします。
1. ターミナルで`az login`を実行して Azure OpenAI Service のリソースの RBAC に登録したアカウントで Azure にログインします。
1. `dotnet run`または再生マークボタンでローカル実行します。
1. ローカルサーバーが起動したら Bot Framework Emulator で`http://localhost:3978/api/messages`に接続します。(ポートが異なる場合があります。環境に合わせてください。)
1. チャットできることを確認します。

### Azure へのデプロイ

#### Azure Web Apps のデプロイとセットアップ

1. Azure Web Apps をデプロイします。
1. システム割当マネージド ID の有効化します。
1. マネージド ID を Azure OpenAI Service のリソースのアクセス制御に「Cognitive Services OpenAI User」として追加します。

#### Azure Bot のデプロイとセットアップ

1. Azure Bot をデプロイします。
   1. データ所在地：「グローバル」
   1. 価格レベル：「Standard」
   1. アプリの種類：「シングルテナント」
   1. 作成の種類：「新しい Microsoft アプリ ID の作成」
1. 「チャンネル」画面の「利用可能なチャンネル」セクションから Teams を有効化します。
1. デプロイした Azure Bot の「構成」画面の「メッセージングエンドポイント」に「{Web Apps の URL}/api/messages」(例：https://xxxx.azurewebsites.net/api/messages)を入力して適用します。
1. 「構成」メニューから以下の情報をメモします。

   - Microsoft App ID
   - アプリ テナント ID

1. Microsoft App ID の横の「パスワードの管理」をクリックして Entra ID のアプリ登録画面へ遷移します。
1. 「新しいクライアントシークレット」をクリックしてシークレットを作成します。作成したシークレットの「値」をメモします。**シークレットの値は作成直後しか表示されません。メモせずに画面遷移をすると２度と表示できないのでご注意ください。**

#### Azure Web Apps のアプリケーション設定

1. Azure Web Apps の「構成」メニューをクリックして「アプリケーション設定」を表示します。
1. 「新しいアプリケーション」をクリックして以下の 6 つのアプリケーション設定を登録します。

   - MicrosoftAppType：SingleTenant
   - MicrosoftAppId：Azure Bot からメモした Microsoft App ID
   - MicrosoftAppPassword：Entra ID のアプリ登録で作成、メモしたクライアントシークレットの値
   - MicrosoftAppTenantId：Azure Bot からメモしたアプリ テナント ID
   - OpenAIEndpoint：Azure OpenAI Service のエンドポイント
   - OpenAIDeployment：Azure OpenAI Service のデプロイ名

1. 「保存」をクリックしてアプリケーション設定を反映します。

#### Azure Web Apps のアプリケーションデプロイ

- 本リポジトリにはデプロイ用の GitHub Actions が含まれています。GitHub Actions を使用してデプロイする場合はそれを使用してください。
- ローカルからデプロイする場合は az cli や VS Code の Azure App Service 拡張機能を使用してデプロイしてください。

### Teams での動作確認

#### Manifest の作成

1. manifest/manifest.json の中の`<xxx>`となっているプレースホルダーをご自身の設定に書き換えます。
1. `manifest.json`、`color.png`、`outline.png`を zip 圧縮します。このときに 3 つのファイルが入っているフォルダを圧縮するのではなく、3 つのファイルを直接圧縮してください。フォルダを圧縮するとインポートできません。

#### Teams へのインポート

##### 個人利用（サイドローディング）

- サイドローディングがサポートされている環境では[アプリをアップロード](https://learn.microsoft.com/ja-jp/microsoftteams/platform/concepts/deploy-and-publish/apps-upload#upload-your-app)して個人用のアプリをインストールすることができます。
- [GitHub Actions](https://github.com/rukasakurai/SystemPromptSwitchingGPTBot/actions)の Artifacts から GitHub Actions で作成されたアプリパッケージをダウンロード可能

##### 組織全体

- 組織全体に配布する場合は Teams の管理者である必要があります。「Teams 管理センター」へアクセスして「Teams アプリ」→「アプリの管理」から「新しいアプリのアップロード」を選択して圧縮した manifest ファイルをアップロードします。

### Sequence Diagram

For a sequence diagram representing a typical user scenario of using this app, please refer to the [Sequence Diagram Documentation](docs/sequence-diagram.md).

## GitHub Copilot Coding Agent

GitHub Copilot Coding Agent を使用して Azure リソースへのアクセスを有効にする方法については、[GitHub Copilot Coding Agent Setup Documentation](docs/copilot-coding-agent-setup.md) を参照してください。

## トラブルシューティング

### ボットが応答しない場合

**最も一般的な原因：Bot Framework のアプリ登録 (App Registration) が削除された、または期限切れ**

Application Insights の Application Map で `login.microsoftonline.com` への呼び出しが失敗している場合は、アプリ登録の問題です。

#### 確認方法

1. Azure ポータル → Microsoft Entra ID → アプリの登録
2. `MicrosoftAppId`（Azure Web Apps の設定値）で検索
3. **アプリが見つからない場合**：削除されています
   - 削除から 30 日以内であれば「削除されたアプリケーション」タブから復元可能
   - または Azure Bot から新しいアプリ登録を作成し、Web Apps の設定を更新
4. **アプリが見つかった場合**：「証明書とシークレット」でクライアントシークレットの有効期限を確認
   - 期限切れの場合は新しいシークレットを作成し、Web Apps の `MicrosoftAppPassword` を更新
