using System;
using System.IO;
using System.Text;
using CFramework.Editor.Configs;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Utilities
{
    /// <summary>
    ///     Luban 配置工程初始化器
    ///     <para>一键创建 Luban 配置所需的目录结构、模板文件和说明文档</para>
    /// </summary>
    public static class LubanProjectInitializer
    {
        /// <summary>
        ///     初始化 Luban 配置工程
        /// </summary>
        public static void Initialize()
        {
            // 让用户选择输出目录（可在项目外部）
            var selectedPath = EditorUtility.OpenFolderPanel("选择 Luban 配置工程目录", "", "");
            if (string.IsNullOrEmpty(selectedPath)) return;

            // 规范化路径
            selectedPath = selectedPath.Replace('\\', '/').TrimEnd('/');

            // 检查目录是否已有 luban.conf
            var confPath = Path.Combine(selectedPath, "luban.conf");
            if (File.Exists(confPath))
            {
                if (!EditorUtility.DisplayDialog("目录已存在配置",
                    $"目录中已存在 luban.conf:\n{confPath}\n\n是否覆盖？", "覆盖", "取消"))
                {
                    return;
                }
            }

            // 检查 Datas 目录
            var datasDir = Path.Combine(selectedPath, "Datas");
            var hasExistingData = Directory.Exists(datasDir) &&
                                  Directory.GetFiles(datasDir).Length > 0;
            if (hasExistingData)
            {
                if (!EditorUtility.DisplayDialog("Datas 目录已存在数据",
                    $"Datas 目录中已有文件，模板文件可能会被覆盖。\n\n是否继续？", "继续", "取消"))
                {
                    return;
                }
            }

            try
            {
                var createdFiles = CreateProjectStructure(selectedPath);
                CreateReadme(selectedPath);

                // 询问是否更新 LubanConfig
                if (EditorUtility.DisplayDialog("初始化完成",
                    BuildSummary(selectedPath, createdFiles) +
                    "\n\n是否更新 Luban 设置指向此配置文件？\n" +
                    "（将在 CFramework → Luban → 设置 中生效）",
                    "更新设置", "稍后手动设置"))
                {
                    UpdateConfigPaths(selectedPath, confPath);
                }

                // 打开目录
                EditorUtility.RevealInFinder(selectedPath);
                Debug.Log($"[Luban] 配置工程初始化完成: {selectedPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Luban] 初始化失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        ///     创建完整的 Luban 配置工程结构
        /// </summary>
        /// <returns>创建的文件数量</returns>
        private static int CreateProjectStructure(string root)
        {
            var datasDir = Path.Combine(root, "Datas");
            Directory.CreateDirectory(datasDir);

            // 创建 luban.conf
            CreateLubanConf(root);

            // 创建模板文件
            CreateSchemaTemplates(datasDir);

            // 创建示例数据表
            CreateDemoTable(datasDir);

            return 6; // conf + 4 schema files + 1 demo table
        }

        /// <summary>
        ///     创建 luban.conf 主配置文件
        /// </summary>
        private static void CreateLubanConf(string root)
        {
            var conf = @"{
    ""groups"":
    [
        {""names"":[""c""], ""default"":true},
        {""names"":[""s""], ""default"":true}
    ],
    ""schemaFiles"":
    [
        {""fileName"":""Datas/Defines.csv"", ""type"":""""},
        {""fileName"":""Datas/__tables__.csv"", ""type"":""table""},
        {""fileName"":""Datas/__beans__.csv"", ""type"":""bean""},
        {""fileName"":""Datas/__enums__.csv"", ""type"":""enum""}
    ],
    ""dataDir"": ""Datas"",
    ""targets"":
    [
        {""name"":""client"", ""manager"":""Tables"", ""groups"":[""c""], ""topModule"":""cfg""},
        {""name"":""server"", ""manager"":""Tables"", ""groups"":[""s""], ""topModule"":""cfg""},
        {""name"":""all"", ""manager"":""Tables"", ""groups"":[""c"",""s""], ""topModule"":""cfg""}
    ],
    ""xargs"":
    [
    ]
}";
            var confPath = Path.Combine(root, "luban.conf");
            File.WriteAllText(confPath, conf, Encoding.UTF8);
        }

        /// <summary>
        ///     创建 Schema 定义模板文件（CSV 格式，Luban 原生支持）
        /// </summary>
        private static void CreateSchemaTemplates(string datasDir)
        {
            // __tables__.csv - 表注册文件
            // Luban 的 table schema 文件格式：name,value_type,comment 等列
            WriteCsv(datasDir, "__tables__.csv", new[]
            {
                "##var:name,value_type,index,comment",
                "##type:string,string,string,string",
                "##",
                "## 在此注册数据表，每行一个表定义",
                "## 示例：TbDemoItem,DemoItem,id,示例物品表",
            });

            // __beans__.csv - Bean（复合数据结构）定义文件
            WriteCsv(datasDir, "__beans__.csv", new[]
            {
                "##var:name,comment",
                "##type:string,string",
                "##",
                "## 在此定义 Bean（复合数据结构），供数据表引用",
                "## 示例：Reward,Reward奖励结构",
            });

            // __enums__.csv - 枚举定义文件
            WriteCsv(datasDir, "__enums__.csv", new[]
            {
                "##var:name,comment",
                "##type:string,string",
                "##",
                "## 在此定义枚举类型",
                "## 示例：ItemType,物品类型枚举",
            });

            // Defines.csv - 常量定义文件
            WriteCsv(datasDir, "Defines.csv", new[]
            {
                "##var:name,value,comment",
                "##type:string,string,string",
                "##",
                "## 在此定义常量，可跨表引用",
            });
        }

        /// <summary>
        ///     创建一个示例数据表，帮助用户理解格式
        /// </summary>
        private static void CreateDemoTable(string datasDir)
        {
            WriteCsv(datasDir, "TbDemoItem.csv", new[]
            {
                "##var:id,name,desc,count",
                "##type:int,string,string,int",
                "##",
                "## 示例物品表",
                "## 可在 __tables__.csv 中添加 TbDemoItem,DemoItem,id,示例物品表 来启用",
                "1,木剑,初始武器,1",
                "2,治疗药水,恢复生命值,5",
                "3,铁盾,基础防御装备,1",
            });
        }

        /// <summary>
        ///     写入 CSV 文件（UTF-8 with BOM，兼容 Excel 中文显示）
        /// </summary>
        private static void WriteCsv(string directory, string fileName, string[] lines)
        {
            var filePath = Path.Combine(directory, fileName);
            // UTF-8 with BOM，确保 Excel 正确识别中文
            var bom = new UTF8Encoding(true);
            File.WriteAllLines(filePath, lines, bom);
        }

        /// <summary>
        ///     创建 README 说明文档
        /// </summary>
        private static void CreateReadme(string root)
        {
            var readme = @"# Luban 配置工程

## 目录结构

```
LubanConfig/
├── luban.conf          主配置文件（定义生成目标、分组等）
├── README.md           本文档
└── Datas/              数据目录
    ├── __tables__.csv  表注册（定义有哪些数据表）
    ├── __beans__.csv   Bean 定义（复合数据结构）
    ├── __enums__.csv   枚举定义
    ├── Defines.csv     常量定义
    └── *.csv           数据表文件（每个表一个文件）
```

## 快速上手

### 1. 注册数据表
在 `__tables__.csv` 中添加行：
```
TbItem,Item,id,物品表
```
- name: 表名（Tb 前缀 + PascalCase）
- value_type: 对应的 Bean 类型名
- index: 主键字段名
- comment: 表描述

### 2. 定义数据结构
在 `__beans__.csv` 中添加行：
```
Item,物品数据结构
```
然后创建 `Item.csv` 定义字段：
```
##var:id,name,desc,quality
##type:int,string,string,int
##,物品ID,物品名称,物品描述,品质
1,木剑,初始武器,1
```

### 3. 创建数据文件
在与 Bean 同名的 CSV 文件中填写数据：
- 第 1 行 `##var:` — 字段名
- 第 2 行 `##type:` — 类型（int, long, float, double, bool, string 等）
- 第 3 行 `##` — 注释行（可选）
- 第 4 行起 — 数据

### 4. 生成代码和数据
在 Unity 中：`CFramework → Luban → 一键生成`

## CSV 格式说明

### 基本类型
| 类型 | 说明 | 示例 |
|------|------|------|
| int | 32位整数 | 100 |
| long | 64位整数 | 999999999 |
| float | 单精度浮点 | 1.5 |
| double | 双精度浮点 | 3.14 |
| bool | 布尔值 | true/false |
| string | 字符串 | 文本内容 |

### 容器类型
| 类型 | 说明 | 示例 |
|------|------|------|
| list<T> | 列表 | list<int> |
| array<T> | 数组 | array<string> |
| map<K,V> | 字典 | map<int,string> |
| set<T> | 集合 | set<int> |

### 特殊标记
- `##group:` 行指定分组（c=客户端, s=服务器, 空=全部）
- 可空类型在类型后加 `?`，如 `int?`
- 分隔符类型：字段名以 `_sep` 后缀，如 `rewards_sep`，值用 `;` 分隔

## 进阶用法

### 多态 Bean
在 __beans__.csv 中定义父类后，可在子 Bean 文件中定义继承关系。

### 引用校验
使用 `ref` 类型可校验字段值是否引用了其他表的合法 key。

### 资源路径校验
使用 `path` 类型可校验资源路径是否存在。

## 注意事项
- CSV 文件建议用 Excel 或专业编辑器打开，避免记事本修改导致编码问题
- 本模板使用 CSV 格式（Luban 原生支持），也可以改用 xlsx 格式
- 如改用 xlsx，需在 luban.conf 的 schemaFiles 中更新 fileName 添加 .xlsx 后缀
";
            File.WriteAllText(Path.Combine(root, "README.md"), readme, Encoding.UTF8);
        }

        /// <summary>
        ///     构建初始化结果摘要
        /// </summary>
        private static string BuildSummary(string root, int fileCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Luban 配置工程已创建于:");
            sb.AppendLine(root);
            sb.AppendLine();
            sb.AppendLine($"已创建 {fileCount} 个文件:");
            sb.AppendLine("  ✓ luban.conf");
            sb.AppendLine("  ✓ Datas/__tables__.csv");
            sb.AppendLine("  ✓ Datas/__beans__.csv");
            sb.AppendLine("  ✓ Datas/__enums__.csv");
            sb.AppendLine("  ✓ Datas/Defines.csv");
            sb.AppendLine("  ✓ Datas/TbDemoItem.csv（示例表）");
            sb.AppendLine("  ✓ README.md");
            return sb.ToString();
        }

        /// <summary>
        ///     更新 LubanConfig 的 ConfPath 指向新创建的配置文件
        /// </summary>
        private static void UpdateConfigPaths(string selectedPath, string confPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);

            // ConfPath: 相对路径或绝对路径
            if (confPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                LubanConfig.ConfPath = confPath.Substring(projectRoot.Length + 1).Replace('\\', '/');
            }
            else
            {
                LubanConfig.ConfPath = confPath.Replace('\\', '/');
            }

            Debug.Log($"[Luban] ConfPath 已更新为: {LubanConfig.ConfPath}");
        }
    }
}
