using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using CFramework.Editor.Configs;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Utilities
{
    /// <summary>
    ///     Luban 配置工程初始化器
    ///     <para>一键创建 Luban 配置所需的目录结构、xlsx 模板文件和说明文档</para>
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
                CreateProjectStructure(selectedPath);
                CreateReadme(selectedPath);

                // 询问是否更新 LubanConfig
                if (EditorUtility.DisplayDialog("初始化完成",
                    BuildSummary(selectedPath) +
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
        private static void CreateProjectStructure(string root)
        {
            var datasDir = Path.Combine(root, "Datas");
            Directory.CreateDirectory(datasDir);

            // 创建 luban.conf
            CreateLubanConf(root);

            // 创建 xlsx 模板文件
            CreateSchemaTemplates(datasDir);

            // 创建示例数据表
            CreateDemoTable(datasDir);
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
        {""fileName"":""Datas/Defines.xlsx"", ""type"":""""},
        {""fileName"":""Datas/__tables__.xlsx"", ""type"":""table""},
        {""fileName"":""Datas/__beans__.xlsx"", ""type"":""bean""},
        {""fileName"":""Datas/__enums__.xlsx"", ""type"":""enum""}
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
        ///     创建 Schema 定义模板文件
        /// </summary>
        private static void CreateSchemaTemplates(string datasDir)
        {
            // __tables__.xlsx - 表注册文件
            WriteXlsx(datasDir, "__tables__.xlsx", new[]
            {
                new[] { "##var:name", "##var:value_type", "##var:index", "##var:comment" },
                new[] { "##type:string", "##type:string", "##type:string", "##type:string" },
                new[] { "##", "", "", "" },
                new[] { "## 在此注册数据表，每行一个表定义", "", "", "" },
                new[] { "## 示例：TbDemoItem,DemoItem,id,示例物品表", "", "", "" },
            });

            // __beans__.xlsx - Bean 定义文件
            WriteXlsx(datasDir, "__beans__.xlsx", new[]
            {
                new[] { "##var:name", "##var:comment" },
                new[] { "##type:string", "##type:string" },
                new[] { "##", "" },
                new[] { "## 在此定义 Bean（复合数据结构），供数据表引用", "" },
                new[] { "## 示例：Reward,Reward奖励结构", "" },
            });

            // __enums__.xlsx - 枚举定义文件
            WriteXlsx(datasDir, "__enums__.xlsx", new[]
            {
                new[] { "##var:name", "##var:comment" },
                new[] { "##type:string", "##type:string" },
                new[] { "##", "" },
                new[] { "## 在此定义枚举类型", "" },
                new[] { "## 示例：ItemType,物品类型枚举", "" },
            });

            // Defines.xlsx - 常量定义文件
            WriteXlsx(datasDir, "Defines.xlsx", new[]
            {
                new[] { "##var:name", "##var:value", "##var:comment" },
                new[] { "##type:string", "##type:string", "##type:string" },
                new[] { "##", "", "" },
                new[] { "## 在此定义常量，可跨表引用", "", "" },
            });
        }

        /// <summary>
        ///     创建一个示例数据表，帮助用户理解格式
        /// </summary>
        private static void CreateDemoTable(string datasDir)
        {
            WriteXlsx(datasDir, "TbDemoItem.xlsx", new[]
            {
                new[] { "##var:id", "##var:name", "##var:desc", "##var:count" },
                new[] { "##type:int", "##type:string", "##type:string", "##type:int" },
                new[] { "##", "", "", "" },
                new[] { "1", "木剑", "初始武器", "1" },
                new[] { "2", "治疗药水", "恢复生命值", "5" },
                new[] { "3", "铁盾", "基础防御装备", "1" },
            });
        }

        /// <summary>
        ///     写入 xlsx 文件（最小化 Open XML 格式，无需第三方依赖）
        /// </summary>
        private static void WriteXlsx(string directory, string fileName, string[][] rows)
        {
            var filePath = Path.Combine(directory, fileName);

            // 构建共享字符串表
            var sharedStrings = new List<string>();
            var sharedStringMap = new Dictionary<string, int>();

            foreach (var row in rows)
            {
                foreach (var cell in row)
                {
                    if (!sharedStringMap.TryGetValue(cell, out _))
                    {
                        sharedStringMap[cell] = sharedStrings.Count;
                        sharedStrings.Add(cell);
                    }
                }
            }

            using var stream = new FileStream(filePath, FileMode.Create);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

            // [Content_Types].xml
            WriteEntry(archive, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/>" +
                "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                "</Types>");

            // _rels/.rels
            WriteEntry(archive, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");

            // xl/workbook.xml
            WriteEntry(archive, "xl/workbook.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                "</workbook>");

            // xl/_rels/workbook.xml.rels
            WriteEntry(archive, "xl/_rels/workbook.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" Target=\"sharedStrings.xml\"/>" +
                "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                "</Relationships>");

            // xl/worksheets/sheet1.xml
            var sheetBuilder = new StringBuilder();
            sheetBuilder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sheetBuilder.Append(
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sheetBuilder.Append("<sheetData>");

            for (var i = 0; i < rows.Length; i++)
            {
                sheetBuilder.Append($"<row r=\"{i + 1}\">");
                for (var j = 0; j < rows[i].Length; j++)
                {
                    var colName = ColumnIndexToName(j);
                    var cellValue = rows[i][j];
                    var sharedIndex = sharedStringMap[cellValue];
                    sheetBuilder.Append(
                        $"<c r=\"{colName}{i + 1}\" t=\"s\"><v>{sharedIndex}</v></c>");
                }

                sheetBuilder.Append("</row>");
            }

            sheetBuilder.Append("</sheetData></worksheet>");
            WriteEntry(archive, "xl/worksheets/sheet1.xml", sheetBuilder.ToString());

            // xl/sharedStrings.xml
            var ssBuilder = new StringBuilder();
            ssBuilder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            ssBuilder.Append(
                $"<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"{sharedStrings.Count}\" uniqueCount=\"{sharedStrings.Count}\">");

            foreach (var s in sharedStrings)
            {
                ssBuilder.Append($"<si><t>{EscapeXml(s)}</t></si>");
            }

            ssBuilder.Append("</sst>");
            WriteEntry(archive, "xl/sharedStrings.xml", ssBuilder.ToString());

            // xl/styles.xml（最小化样式表）
            WriteEntry(archive, "xl/styles.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<fonts count=\"1\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                "<fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills>" +
                "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                "<cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/></cellXfs>" +
                "</styleSheet>");
        }

        /// <summary>
        ///     列索引转 Excel 列名（0→A, 1→B, ... 25→Z, 26→AA）
        /// </summary>
        private static string ColumnIndexToName(int index)
        {
            var name = "";
            var i = index;
            while (i >= 0)
            {
                name = (char)('A' + i % 26) + name;
                i = i / 26 - 1;
            }

            return name;
        }

        /// <summary>
        ///     XML 特殊字符转义
        /// </summary>
        private static string EscapeXml(string text)
        {
            return text.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        /// <summary>
        ///     向 zip 中写入文本条目
        /// </summary>
        private static void WriteEntry(ZipArchive archive, string entryName, string content)
        {
            var entry = archive.CreateEntry(entryName, System.IO.Compression.CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            var bytes = Encoding.UTF8.GetBytes(content);
            entryStream.Write(bytes, 0, bytes.Length);
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
├── luban.conf              主配置文件（定义生成目标、分组等）
├── README.md               本文档
└── Datas/                  数据目录
    ├── __tables__.xlsx     表注册（定义有哪些数据表）
    ├── __beans__.xlsx      Bean 定义（复合数据结构）
    ├── __enums__.xlsx      枚举定义
    ├── Defines.xlsx        常量定义
    └── *.xlsx              数据表文件（每个表一个文件）
```

## 快速上手

### 1. 注册数据表
在 `__tables__.xlsx` 中添加行：
```
TbItem,Item,id,物品表
```
- name: 表名（Tb 前缀 + PascalCase）
- value_type: 对应的 Bean 类型名
- index: 主键字段名
- comment: 表描述

### 2. 定义数据结构
在 `__beans__.xlsx` 中添加行：
```
Item,物品数据结构
```
然后创建 `Item.xlsx` 定义字段：
```
##var:id,name,desc,quality
##type:int,string,string,int
##,物品ID,物品名称,物品描述,品质
1,木剑,初始武器,1
```

### 3. 创建数据文件
在与 Bean 同名的 xlsx 文件中填写数据：
- 第 1 行 `##var:` — 字段名
- 第 2 行 `##type:` — 类型（int, long, float, double, bool, string 等）
- 第 3 行 `##` — 注释行（可选）
- 第 4 行起 — 数据

### 4. 生成代码和数据
在 Unity 中：`CFramework → Luban → 一键生成`

## 类型说明

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
| list&lt;T&gt; | 列表 | list&lt;int&gt; |
| array&lt;T&gt; | 数组 | array&lt;string&gt; |
| map&lt;K,V&gt; | 字典 | map&lt;int,string&gt; |
| set&lt;T&gt; | 集合 | set&lt;int&gt; |

### 特殊标记
- `##group:` 行指定分组（c=客户端, s=服务器, 空=全部）
- 可空类型在类型后加 `?`，如 `int?`
- 分隔符类型：字段名以 `_sep` 后缀，如 `rewards_sep`，值用 `;` 分隔

## 进阶用法

### 多态 Bean
在 __beans__.xlsx 中定义父类后，可在子 Bean 文件中定义继承关系。

### 引用校验
使用 `ref` 类型可校验字段值是否引用了其他表的合法 key。

### 资源路径校验
使用 `path` 类型可校验资源路径是否存在。

## 注意事项
- 数据文件使用 Excel 编辑
- luban.conf 中的 schemaFiles 后缀必须与实际文件一致（.xlsx）
";
            File.WriteAllText(Path.Combine(root, "README.md"), readme, Encoding.UTF8);
        }

        /// <summary>
        ///     构建初始化结果摘要
        /// </summary>
        private static string BuildSummary(string root)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Luban 配置工程已创建于:");
            sb.AppendLine(root);
            sb.AppendLine();
            sb.AppendLine("已创建文件:");
            sb.AppendLine("  ✓ luban.conf");
            sb.AppendLine("  ✓ Datas/__tables__.xlsx");
            sb.AppendLine("  ✓ Datas/__beans__.xlsx");
            sb.AppendLine("  ✓ Datas/__enums__.xlsx");
            sb.AppendLine("  ✓ Datas/Defines.xlsx");
            sb.AppendLine("  ✓ Datas/TbDemoItem.xlsx（示例表）");
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
