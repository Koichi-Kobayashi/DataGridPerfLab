# DataGridPerfLab

**.NET 6 ～ .NET 10** における **WPF DataGrid の性能比較ラボ**です。

---

## 📊 結果まとめ

### 1. 機能別 体感差まとめ

| 比較項目 | 設定 | 体感速度 | メモリ割当 | 備考 |
|---|---|---|---|---|
| ItemsSource 更新 | Clear + Add | 遅い | 多い | CollectionChanged 多発 |
|  | ItemsSource 差し替え | 速い | 少ない | **最重要最適化** |
| 仮想化 | OFF | 非常に遅い | 非常に多い | 100k 行で UI 固定 |
|  | ON (Recycling) | 快適 | 少ない | DataGrid 必須 |
| Filter / Sort / Group | DeferRefresh OFF | もっさり | 多い | 毎回再評価 |
|  | DeferRefresh ON | 速い | 少ない | 正しい使い方 |
| LiveShaping | OFF | 安定 | 少ない | 手動更新 |
|  | ON | 不安定 | 増加 | デモ向け |
| Group + LiveShaping | 両方 ON | 重い | 非常に多い | 教材用途 |
| .NET バージョン | 6 → 10 | 改善 | 微減 | 内部最適化の積み重ね |

---

### 2. .NET バージョン別 実測記入表

| .NET | Rebuild 100k (ms) | ApplyView (ms) | Mutate 5k (ms) | 備考 |
|---|---:|---:|---:|---|
| net6 | | | | |
| net7 | | | | |
| net8 | | | | |
| net9 | | | | |
| net10 | | | | |

---

### 3. 構成別 おすすめ度

| 構成 | 実用 | デモ | コメント |
|---|---|---|---|
| 仮想化 ON + Batch ON | ★★★★★ | ★★★★ | 実運用の基本 |
| 仮想化 OFF | ★ | ★★★★★ | 最悪ケース比較 |
| DeferRefresh ON | ★★★★★ | ★★★ | View 更新の正解 |
| LiveShaping ON | ★★ | ★★★★★ | 視覚的に分かりやすい |
| Group + LiveShaping | ★ | ★★★★★ | 限界を見せる構成 |

---

## 結論

WPF DataGrid の性能は **ランタイムの新旧よりも使い方が支配的**です。
仮想化・ItemsSource 差し替え・DeferRefresh を正しく使うことが、
.NET のバージョンアップ以上に効果を発揮します。

---

## 並列生成（System.Collections.Concurrent）

UI 更新を速くするというより、**UI に渡す前のデータ生成（CPU作業）**を並列化するためのトグルです。生成後は ItemsSource を一括差し替えするのが基本です。


---

## 生成方式（BuildMode）

データ生成（CPU作業）部分の比較用に、以下の 3 パターンを切り替えできます。

- **逐次 (Sequential)**: 1スレッドで生成
- **並列 (ConcurrentBag)**: `System.Collections.Concurrent.ConcurrentBag` に並列で追加（順序が崩れるため最後にソート）
- **並列 (Array)**: `Item[]` に index で書き込む方式（共有コレクションが無く、並列生成としては速いことが多い）

※ DataGrid の描画は UI スレッドが支配的なので、これは主に「生成コストの比較」です。UI 反映は ItemsSource 差し替えが基本です。
