# Git履歴から大容量ファイルを削除する手順

このリポジトリには、GitHubの100MBファイルサイズ制限を超える大容量ファイル（.abc）が含まれています。
これらのファイルを履歴から完全に削除する必要があります。

## 問題のあるファイル

以下のファイルが履歴に含まれています（各205-206MB）：
- `Unity_BatchRenderingExample/Assets/TestRec/SampleScene_Recorder_Take02.abc`
- `Unity_BatchRenderingExample/Recordings/AlembicRecorder.abc`
- `Unity_BatchRenderingExample/Recordings/SampleScene_Take01.abc`
- `Unity_BatchRenderingExample/Assets/TestRec/AlembicRecorder.abc`

## 削除手順

### 方法1: git filter-branch を使用（推奨）

リポジトリのルートディレクトリ（`/mnt/d/Works/Unity_BatchRecorderTool/`）から以下のコマンドを実行してください：

```bash
# 1. バックアップを作成
git clone --mirror . ../Unity_BatchRecorderTool_backup

# 2. 大容量ファイルを履歴から削除
FILTER_BRANCH_SQUELCH_WARNING=1 git filter-branch --force --index-filter \
  'git rm --cached --ignore-unmatch \
   Unity_BatchRenderingExample/Assets/TestRec/SampleScene_Recorder_Take02.abc \
   Unity_BatchRenderingExample/Recordings/AlembicRecorder.abc \
   Unity_BatchRenderingExample/Recordings/SampleScene_Take01.abc \
   Unity_BatchRenderingExample/Assets/TestRec/AlembicRecorder.abc' \
  --prune-empty --tag-name-filter cat -- --all

# 3. クリーンアップ
rm -rf .git/refs/original/
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# 4. リモートリポジトリを更新（注意：破壊的な操作です）
git push --force-with-lease --all
git push --force-with-lease --tags
```

### 方法2: BFG Repo-Cleaner を使用（より簡単）

1. BFG Repo-Cleanerをダウンロード: https://rtyley.github.io/bfg-repo-cleaner/

2. 以下のコマンドを実行：

```bash
# バックアップを作成
git clone --mirror . ../Unity_BatchRecorderTool_backup

# BFGで大容量ファイルを削除（100MB以上のファイル）
java -jar bfg.jar --strip-blobs-bigger-than 100M .

# クリーンアップ
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# リモートリポジトリを更新
git push --force-with-lease --all
git push --force-with-lease --tags
```

### 方法3: git filter-repo を使用（最新のツール）

1. git-filter-repoをインストール：
```bash
pip install git-filter-repo
```

2. 以下のコマンドを実行：

```bash
# 特定のファイルを削除
git filter-repo --path Unity_BatchRenderingExample/Assets/TestRec/SampleScene_Recorder_Take02.abc --invert-paths
git filter-repo --path Unity_BatchRenderingExample/Recordings/AlembicRecorder.abc --invert-paths
git filter-repo --path Unity_BatchRenderingExample/Recordings/SampleScene_Take01.abc --invert-paths
git filter-repo --path Unity_BatchRenderingExample/Assets/TestRec/AlembicRecorder.abc --invert-paths
```

## 注意事項

1. **必ずバックアップを作成してから実行してください**
2. **force pushは破壊的な操作です** - 他の開発者と作業している場合は事前に通知してください
3. 実行後、他の開発者は以下のコマンドでローカルリポジトリを更新する必要があります：
   ```bash
   git fetch --all
   git reset --hard origin/main
   ```

## .gitignoreの更新

`.gitignore`ファイルは既に更新済みで、以下が追加されています：
- `/Recordings/` - 録画出力ディレクトリ全体
- `*.abc` - Alembicファイル
- `*.exr` - EXR画像ファイル

これにより、今後これらのファイルがGitに追加されることはありません。