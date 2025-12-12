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

## トラブルシューティング

### ボットが応答しない場合

ボットがメッセージに応答しない場合、以下の項目を確認してください：

#### 1. Azure OpenAI Service の接続確認

- **エンドポイント URL の確認**: Azure Web Apps の「構成」→「アプリケーション設定」で `OpenAIEndpoint` が正しく設定されているか確認してください。
- **デプロイ名の確認**: `OpenAIDeployment` が実際のデプロイ名と一致しているか確認してください。
- **マネージド ID の権限確認**: Azure Web Apps のマネージド ID が Azure OpenAI Service のリソースで「Cognitive Services OpenAI User」ロールを持っているか確認してください。
- **Azure OpenAI Service の状態確認**: Azure ポータルで Azure OpenAI Service が正常に動作しているか確認してください。

#### 2. Bot Framework 認証の確認

- **クライアントシークレットの有効期限**: Entra ID のアプリ登録で作成したクライアントシークレットの有効期限が切れていないか確認してください。有効期限が切れている場合は新しいシークレットを作成し、Azure Web Apps の `MicrosoftAppPassword` を更新してください。
- **Microsoft App ID の確認**: Azure Web Apps の `MicrosoftAppId` が Azure Bot の Microsoft App ID と一致しているか確認してください。
- **メッセージングエンドポイントの確認**: Azure Bot の「構成」画面で「メッセージングエンドポイント」が正しく設定されているか確認してください（例: `https://your-app.azurewebsites.net/api/messages`）。

#### 3. ログの確認

##### Azure Web Apps のログ

1. **ログストリーム（リアルタイム）**
   - Azure ポータル → Azure Web Apps → 「監視」→「ログストリーム」
   - リアルタイムでアプリケーションログとシステムログを確認できます
   - ボットにメッセージを送信しながら、エラーが表示されるか確認してください

2. **診断設定の有効化**
   - Azure ポータル → Azure Web Apps → 「監視」→「診断設定」
   - 以下のログを有効にすることを推奨します：
     - **アプリケーションログ**: `Information` レベル以上
     - **詳細なエラーメッセージ**: 有効
     - **失敗した要求のトレース**: 有効
     - **Web サーバーログ**: 有効

3. **ログファイルの確認**
   - Azure ポータル → Azure Web Apps → 「開発ツール」→「高度なツール (Kudu)」→「Go」
   - Kudu コンソールで「Debug console」→「CMD」を選択
   - `LogFiles` フォルダ内のログファイルを確認：
     - `Application/`: アプリケーションログ
     - `DetailedErrors/`: 詳細なエラー情報
     - `http/`: HTTP リクエストログ

##### Application Insights（推奨）

Application Insights を有効にすると、より詳細な診断が可能です：

1. **Application Insights の有効化**
   - Azure ポータル → Azure Web Apps → 「設定」→「Application Insights」
   - 「Application Insights を有効にする」を選択

2. **確認すべきログの種類**

   **例外ログ（Exceptions）**
   - Azure ポータル → Application Insights → 「調査」→「エラー」
   - `[OnTurnError]` や `Azure OpenAI request failed` などのエラーを確認
   - クエリ例：
     ```kusto
     exceptions
     | where timestamp > ago(1d)
     | where cloud_RoleName == "your-web-app-name"
     | order by timestamp desc
     ```

   **トレースログ（Traces）**
   - Azure ポータル → Application Insights → 「監視」→「ログ」
   - アプリケーションが出力した `ILogger` のログを確認
   - クエリ例：
     ```kusto
     traces
     | where timestamp > ago(1d)
     | where message contains "OpenAI" or message contains "OnTurnError"
     | order by timestamp desc
     ```

   **依存関係の失敗（Dependencies）**
   - Azure OpenAI Service への呼び出しの成功/失敗を確認
   - クエリ例：
     ```kusto
     dependencies
     | where timestamp > ago(1d)
     | where name contains "openai" or target contains "openai"
     | where success == false
     | order by timestamp desc
     ```

   **要求ログ（Requests）**
   - Bot Framework からの HTTP リクエストを確認
   - クエリ例：
     ```kusto
     requests
     | where timestamp > ago(1d)
     | where url contains "/api/messages"
     | where success == false
     | order by timestamp desc
     ```

##### Azure Bot Service のログ

1. **Bot Analytics**
   - Azure ポータル → Azure Bot → 「Analytics」
   - メッセージ数やチャンネルごとの統計を確認できます

2. **チャンネルの状態確認**
   - Azure ポータル → Azure Bot → 「チャンネル」
   - Teams チャンネルが「実行中」状態になっているか確認

##### Log Analytics（統合ログ分析）

複数のリソースのログを統合して分析する場合：

1. **診断設定で Log Analytics への送信を有効化**
   - Azure Web Apps → 「監視」→「診断設定」→「診断設定を追加する」
   - すべてのログカテゴリを選択
   - 送信先: Log Analytics ワークスペース

2. **Log Analytics でのクエリ例**
   ```kusto
   // すべてのエラーを検索
   AppServiceConsoleLogs
   | where TimeGenerated > ago(1d)
   | where ResultDescription contains "error" or ResultDescription contains "exception"
   | order by TimeGenerated desc
   
   // OpenAI 関連のログを検索
   AppServiceConsoleLogs
   | where TimeGenerated > ago(1d)
   | where ResultDescription contains "OpenAI" or ResultDescription contains "Azure.AI.OpenAI"
   | order by TimeGenerated desc
   ```

##### 確認すべき主なエラーパターン

1. **認証エラー**
   - `401 Unauthorized`: Bot または Azure OpenAI の認証に失敗
   - `403 Forbidden`: 権限不足（RBAC の確認が必要）

2. **接続エラー**
   - `RequestFailedException`: Azure OpenAI Service への接続失敗
   - `TimeoutException`: タイムアウト

3. **設定エラー**
   - `NullReferenceException`: 設定値が未設定または不正
   - `DeploymentNotFound`: OpenAI のデプロイ名が存在しない

#### 4. Web App の再起動

設定を変更した後は、Azure Web Apps を再起動して変更を反映させてください。

#### 5. Bot Framework Emulator でのテスト

ローカル環境で Bot Framework Emulator を使用してボットをテストし、問題を切り分けることができます。

### よくあるエラーメッセージ

- **「Azure OpenAI サービスへの接続に失敗しました」**: Azure OpenAI Service の認証または接続に問題があります。マネージド ID の権限とエンドポイント URL を確認してください。
- **「AIからの応答を取得できませんでした」**: Azure OpenAI Service からの応答が不正です。デプロイ名とモデルの状態を確認してください。
- **「システムプロンプトの設定が見つかりませんでした」**: システムプロンプトの設定に問題があります。コードの設定を確認してください。
