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
    ///     <para>一键创建 Luban 配置所需的目录结构、模板文件和说明文档</para>
    /// </summary>
    public static class LubanProjectInitializer
    {
        /// <summary>
        ///     初始化 Luban 配置工程
        /// </summary>
        public static void Initialize()
        {
            // 选择配置文件格式
            var useXlsx = false;
            var selected = EditorUtility.DisplayDialogComplex("选择配置文件格式",
                "请选择 Luban 配置文件的数据格式：\n\n" +
                "• CSV（推荐）— 所有 Luban 版本均支持，纯文本格式\n" +
                "• XLSX — Excel 原生格式，需完整版 Luban（含 ExcelSchemaLoader）",
                "CSV（推荐）", "XLSX", "取消");

            if (selected == 2) return; // 取消
            useXlsx = selected == 1;

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
                CreateProjectStructure(selectedPath, useXlsx);
                CreateReadme(selectedPath, useXlsx);

                // 询问是否更新 LubanConfig
                if (EditorUtility.DisplayDialog("初始化完成",
                    BuildSummary(selectedPath, useXlsx) +
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
        private static void CreateProjectStructure(string root, bool useXlsx)
        {
            var datasDir = Path.Combine(root, "Datas");
            var definesDir = Path.Combine(root, "Defines");
            Directory.CreateDirectory(datasDir);
            Directory.CreateDirectory(definesDir);

            CreateLubanConf(root, useXlsx);
            CreateBuiltinDefines(definesDir);

            if (useXlsx)
            {
                CreateXlsxSchemaTemplates(datasDir);
                CreateXlsxDemoTable(datasDir);
            }
            else
            {
                CreateCsvSchemaTemplates(datasDir);
                CreateCsvDemoTable(datasDir);
            }
        }

        /// <summary>
        ///     创建 luban.conf 主配置文件
        /// </summary>
        private static void CreateLubanConf(string root, bool useXlsx)
        {
            var ext = useXlsx ? ".xlsx" : ".csv";
            var conf = @"{
    ""groups"":
    [
        {""names"":[""c""], ""default"":true},
        {""names"":[""s""], ""default"":true}
    ],
    ""schemaFiles"":
    [
        {""fileName"":""Defines"", ""type"":""""},
        {""fileName"":""Datas/__tables__" + ext + @""", ""type"":""table""},
        {""fileName"":""Datas/__beans__" + ext + @""", ""type"":""bean""},
        {""fileName"":""Datas/__enums__" + ext + @""", ""type"":""enum""}
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
        ///     创建 Defines 目录下的内置类型定义
        ///     <para>定义常用值类型（vector2/vector3/vector4），与官方 luban_examples 保持一致</para>
        /// </summary>
        private static void CreateBuiltinDefines(string definesDir)
        {
            var xml = @"<module name="""">
    <bean name=""vector2"" valueType=""1"" sep="","">
        <var name=""x"" type=""float""/>
        <var name=""y"" type=""float""/>
    </bean>
    <bean name=""vector3"" valueType=""1"" sep="","">
        <var name=""x"" type=""float""/>
        <var name=""y"" type=""float""/>
        <var name=""z"" type=""float""/>
    </bean>
    <bean name=""vector4"" valueType=""1"" sep="","">
        <var name=""x"" type=""float""/>
        <var name=""y"" type=""float""/>
        <var name=""z"" type=""float""/>
        <var name=""w"" type=""float""/>
    </bean>
</module>";
            File.WriteAllText(Path.Combine(definesDir, "builtin.xml"), xml, Encoding.UTF8);
        }

        #region CSV 模板

        private static void CreateCsvSchemaTemplates(string datasDir)
        {
            // 表定义：full_name(必填), value_type(必填), input(必填), output(必填),
            //         read_schema_from_file, index, mode, comment, group, tags
            WriteCsv(datasDir, "__tables__.csv", new[]
            {
                "##var:full_name,value_type,read_schema_from_file,input,index,mode,group,comment,tags,output",
                "##type:string,string,bool,string,string,string,string,string,string,string",
                "##,全名,记录类型,从文件读Schema,数据文件,主键,模式,分组,注释,标签,输出文件名",
                "TbDemoItem,DemoItem,true,TbDemoItem.csv,id,,,,示例物品表,,TbDemoItem",
            });

            WriteCsv(datasDir, "__beans__.csv", new[]
            {
                "##var:full_name,parent,valueType,alias,sep,comment,tags,group,*fields.name,*fields.alias,*fields.type,*fields.group,*fields.comment,*fields.tags",
                "##type:string,string,bool,string,string,string,string,string,string,string,string,string,string,string",
                "##,全名,父类,是否值类型,别名,分隔符,注释,标签,分组,字段名,别名,类型,分组,注释,标签",
            });

            WriteCsv(datasDir, "__enums__.csv", new[]
            {
                "##var:full_name,flags,unique,group,comment,tags,*items.name,*items.alias,*items.value,*items.comment,*items.tags",
                "##type:string,bool,bool,string,string,string,string,string,string,string,string",
                "##,全名,是否标志位,枚举值唯一,分组,注释,标签,枚举名,别名,值,注释,标签",
            });
        }

        private static void CreateCsvDemoTable(string datasDir)
        {
            WriteCsv(datasDir, "TbDemoItem.csv", new[]
            {
                "##var:id,name,desc,count",
                "##type:int,string,string,int",
                "##,物品ID,物品名称,物品描述,数量",
                "1,木剑,初始武器,1",
                "2,治疗药水,恢复生命值,5",
                "3,铁盾,基础防御装备,1",
            });
        }

        private static void WriteCsv(string directory, string fileName, string[] lines)
        {
            var filePath = Path.Combine(directory, fileName);
            var bom = new UTF8Encoding(true);
            File.WriteAllLines(filePath, lines, bom);
        }

        #endregion

        #region XLSX 模板

        private static void CreateXlsxSchemaTemplates(string datasDir)
        {
            // Schema 表格式（__tables__/__beans__/__enums__）：
            // - 第一列(A列)为行标记：##var、##
            // - 字段名/数据从第二列(B列)开始
            // - 没有 ##type 行（Luban 内置知道每个字段的类型）
            WriteXlsx(datasDir, "__tables__.xlsx", new[]
            {
                new[] { "##var", "full_name", "value_type", "read_schema_from_file", "input", "index", "mode", "group", "comment", "tags", "output" },
                new[] { "##", "全名", "记录类型", "从文件读Schema", "数据文件", "主键", "模式", "分组", "注释", "标签", "输出文件名" },
                new[] { "", "TbDemoItem", "DemoItem", "true", "TbDemoItem.xlsx", "id", "", "", "示例物品表", "", "TbDemoItem" },
            });

            // __beans__ 字段（对照官方 luban_examples）：
            // Row 1: 顶级字段，*fields 标记多记录列
            // Row 2: *fields 的子字段定义，从 B 列开始（无空列填充）
            WriteXlsx(datasDir, "__beans__.xlsx", new[]
            {
                new[] { "##var", "full_name", "parent", "valueType", "alias", "sep", "comment", "tags", "group", "*fields", "", "", "", "", "", "" },
                new[] { "##var", "name", "alias", "type", "group", "comment", "tags", "variants" },
                new[] { "##", "全名(包含模块和名字)", "父类", "是否值类型", "别名", "分隔符", "注释", "标签", "分组", "字段名", "别名", "类型", "分组", "注释", "标签", "多态" },
            });

            // __enums__ 字段（对照官方 luban_examples）：
            // Row 1: 顶级字段，*items 标记多记录列
            // Row 2: *items 的子字段定义，从 B 列开始（无空列填充）
            WriteXlsx(datasDir, "__enums__.xlsx", new[]
            {
                new[] { "##var", "full_name", "flags", "unique", "group", "comment", "tags", "*items", "", "", "", "" },
                new[] { "##var", "name", "alias", "value", "comment", "tags" },
                new[] { "##", "全名(包含模块和名字)", "是否标志位", "枚举值唯一", "分组", "注释", "标签", "枚举名", "别名", "值", "注释", "标签" },
            });
        }

        private static void CreateXlsxDemoTable(string datasDir)
        {
            // 数据表格式：
            // - 第一列(A列)为行标记：##var、##type、##
            // - 字段名/类型/数据从第二列(B列)开始
            WriteXlsx(datasDir, "TbDemoItem.xlsx", new[]
            {
                new[] { "##var", "id", "name", "desc", "count" },
                new[] { "##type", "int", "string", "string", "int" },
                new[] { "##", "物品ID", "物品名称", "物品描述", "数量" },
                new[] { "", "1", "木剑", "初始武器", "1" },
                new[] { "", "2", "治疗药水", "恢复生命值", "5" },
                new[] { "", "3", "铁盾", "基础防御装备", "1" },
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

        private static string EscapeXml(string text)
        {
            return text.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private static void WriteEntry(ZipArchive archive, string entryName, string content)
        {
            var entry = archive.CreateEntry(entryName, System.IO.Compression.CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            var bytes = Encoding.UTF8.GetBytes(content);
            entryStream.Write(bytes, 0, bytes.Length);
        }

        #endregion

        /// <summary>
        ///     创建 README 说明文档
        /// </summary>
        private static void CreateReadme(string root, bool useXlsx)
        {
            var ext = useXlsx ? ".xlsx" : ".csv";

            var readme = @"# Luban 配置工程

## 目录结构

```
LubanConfig/
├── luban.conf              主配置文件（定义生成目标、分组等）
├── README.md               本文档
├── Defines/                类型定义目录（XML 格式）
│   └── builtin.xml         内置类型（vector2/vector3/vector4）
└── Datas/                  数据目录
    ├── __tables__." + ext + @"     表注册（定义有哪些数据表）
    ├── __beans__." + ext + @"      Bean 定义（可选，read_schema_from_file=true 时不需要）
    ├── __enums__." + ext + @"      枚举定义
    └── *." + ext + @"              数据表文件（每个表一个文件）
```

## 快速上手

### 方式一：简单模式（推荐，read_schema_from_file=true）

类型定义直接写在数据文件的标题行中，无需单独定义 Bean。

#### 1. 创建数据文件
创建 `TbItem." + ext + @"`：
```
##var:id,name,desc,quality
##type:int,string,string,int
##,物品ID,物品名称,物品描述,品质
1,木剑,初始武器,1
```

#### 2. 注册表
在 `__tables__." + ext + @"` 中添加行：
```
TbItem,Item,TbItem,true,id,物品表,TbItem
```
字段说明：full_name, value_type, input, read_schema_from_file, index, comment, output

### 方式二：标准模式（read_schema_from_file=false）

Bean 类型在 `__beans__` 中统一定义，适合复杂项目。

### 生成代码和数据
在 Unity 中：`CFramework → Luban → 一键生成`

## __tables__ 字段说明

| 字段 | 必填 | 类型 | 说明 |
|------|------|------|------|
| full_name | 是 | string | 表全名（如 TbItem） |
| value_type | 是 | string | 记录类型名（如 Item） |
| input | 是 | string | 数据文件路径（相对 dataDir） |
| read_schema_from_file | 否 | bool | true=从数据文件标题行读取类型定义 |
| index | 否 | string | 主键字段名，联合主键用+，独立主键用, |
| mode | 否 | string | one(单例)/map(默认)/list |
| comment | 否 | string | 注释 |
| group | 否 | string | 分组 |
| output | 否 | string | 输出文件名 |

## 数据文件格式

- 第 1 行 `##var:` — 字段名
- 第 2 行 `##type:` — 类型（int, long, float, double, bool, string 等）
- 第 3 行 `##` — 注释行（可选）
- 第 4 行起 — 数据

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
在 __beans__." + ext + @" 中定义父类后，可在子 Bean 文件中定义继承关系。

### 引用校验
使用 `ref` 类型可校验字段值是否引用了其他表的合法 key。

### 资源路径校验
使用 `path` 类型可校验资源路径是否存在。

## 注意事项
- luban.conf 中的 schemaFiles 后缀必须与实际文件一致
- 如需切换格式，需同时更新 luban.conf 后缀和 Datas 目录中的文件
- 使用 read_schema_from_file=true 时，不要在 __beans__ 中重复定义同名 Bean
";
            File.WriteAllText(Path.Combine(root, "README.md"), readme, Encoding.UTF8);
        }

        /// <summary>
        ///     构建初始化结果摘要
        /// </summary>
        private static string BuildSummary(string root, bool useXlsx)
        {
            var ext = useXlsx ? ".xlsx" : ".csv";
            var sb = new StringBuilder();
            sb.AppendLine("Luban 配置工程已创建于:");
            sb.AppendLine(root);
            sb.AppendLine();
            sb.AppendLine($"格式: {ext.TrimStart('.').ToUpper()}");
            sb.AppendLine();
            sb.AppendLine("已创建文件:");
            sb.AppendLine("  ✓ luban.conf");
            sb.AppendLine("  ✓ Defines/builtin.xml（内置类型：vector2/vector3/vector4）");
            sb.AppendLine($"  ✓ Datas/__tables__{ext}");
            sb.AppendLine($"  ✓ Datas/__beans__{ext}");
            sb.AppendLine($"  ✓ Datas/__enums__{ext}");
            sb.AppendLine($"  ✓ Datas/TbDemoItem{ext}（示例表）");
            sb.AppendLine("  ✓ README.md");
            return sb.ToString();
        }

        /// <summary>
        ///     更新 LubanConfig 的 ConfPath 指向新创建的配置文件
        /// </summary>
        private static void UpdateConfigPaths(string selectedPath, string confPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);

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
