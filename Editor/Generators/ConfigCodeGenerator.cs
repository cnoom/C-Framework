using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CFramework.Editor.Generators
{
    /// <summary>
    ///     值类型字段定义（共享数据结构）
    ///     用于 ConfigCreatorWindow（Odin版）、ConfigCreatorWindowDefault（UIToolkit版）
    ///     和 DashboardConfigCreatorTab 之间共享字段配置数据
    /// </summary>
    [Serializable]
    public sealed class ValueField
    {
#if ODIN_INSPECTOR
        [LabelText("字段名")] [Tooltip("字段名称")] [Required]
#endif
        public string fieldName;

#if ODIN_INSPECTOR
        [LabelText("类型")] [ValueDropdown(nameof(FieldTypeOptions))]
#endif
        public string fieldType = "int";

#if ODIN_INSPECTOR
        [LabelText("主键")] [Tooltip("是否作为主键字段")]
#endif
        public bool isKeyField;

#if ODIN_INSPECTOR
        [LabelText("描述")] [Tooltip("字段描述（用于注释）")] [TextArea(1, 2)]
#endif
        public string description;

#if ODIN_INSPECTOR
        private IEnumerable<string> FieldTypeOptions = new[]
        {
            "int", "float", "string", "bool", "long", "double",
            "Vector2", "Vector3", "Vector4", "Color",
            "GameObject", "Transform", "Sprite", "Texture", "AudioClip"
        };
#endif
    }

    /// <summary>
    ///     配置表代码生成器共享工具
    ///     统一管理配置表/数据类的代码生成逻辑，消除 ConfigCreatorWindow（Odin版）、
    ///     ConfigCreatorWindowDefault（UIToolkit版）和 DashboardConfigCreatorTab 之间的代码重复
    /// </summary>
    public static class ConfigCodeGenerator
    {
        #region 公开方法

        /// <summary>
        ///     生成数据类代码
        /// </summary>
        public static string GenerateDataClassCode(
            string valueTypeName,
            string keyType,
            string dataNamespace,
            List<ValueField> valueFields)
        {
            var sb = new StringBuilder();
            var keyField = valueFields.Find(f => f.isKeyField);
            if (keyField == null && valueFields.Count > 0) keyField = valueFields[0];

            sb.AppendLine("using System;");
            sb.AppendLine("using CFramework;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(dataNamespace))
            {
                sb.AppendLine($"namespace {dataNamespace}");
                sb.AppendLine("{");
            }

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {valueTypeName} 数据结构");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    [Serializable]");
            sb.AppendLine($"    public sealed class {valueTypeName} : IConfigItem<{keyType}>");
            sb.AppendLine("    {");

            foreach (var field in valueFields)
            {
                if (!string.IsNullOrEmpty(field.description))
                {
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// {field.description}");
                    sb.AppendLine("        /// </summary>");
                }

                sb.Append($"        public {field.fieldType} {field.fieldName}");

                if (field.fieldType == "string")
                    sb.AppendLine(" = \"\";");
                else if (field.fieldType == "bool")
                    sb.AppendLine(" = false;");
                else if (IsNumericType(field.fieldType))
                    sb.AppendLine(" = 0;");
                else
                    sb.AppendLine(";");

                sb.AppendLine();
            }

            if (keyField != null)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// 配置数据主键");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public {keyType} Key => {keyField.fieldName};");
                sb.AppendLine();
            }

            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 克隆当前对象");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public {valueTypeName} Clone()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return new {valueTypeName}");
            sb.AppendLine("            {");

            for (var i = 0; i < valueFields.Count; i++)
            {
                var field = valueFields[i];
                sb.Append($"                {field.fieldName} = {field.fieldName}");
                sb.AppendLine(i < valueFields.Count - 1 ? "," : "");
            }

            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("    }");

            if (!string.IsNullOrEmpty(dataNamespace)) sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        ///     生成配置表类代码
        /// </summary>
        public static string GenerateConfigClassCode(
            string configName,
            string configNamespace,
            string dataNamespace,
            string keyType,
            string valueTypeName)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using CFramework;");
            sb.AppendLine("using UnityEngine;");

            if (!string.IsNullOrEmpty(dataNamespace) && dataNamespace != configNamespace)
                sb.AppendLine($"using {dataNamespace};");

            sb.AppendLine();

            if (!string.IsNullOrEmpty(configNamespace))
            {
                sb.AppendLine($"namespace {configNamespace}");
                sb.AppendLine("{");
            }

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {configName} 配置表");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(
                $"    [CreateAssetMenu(fileName = \"{configName}\", menuName = \"Game/Config/{configName}\")]");
            sb.AppendLine(
                $"    public sealed class {configName} : ConfigTableAsset<{keyType}, {valueTypeName}>");
            sb.AppendLine("    {");
            sb.AppendLine("        // 数据在 Inspector 中配置");
            sb.AppendLine("    }");

            if (!string.IsNullOrEmpty(configNamespace)) sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        ///     将生成的脚本文件写入磁盘
        /// </summary>
        public static void WriteScriptFiles(
            string configOutputPath,
            string dataOutputPath,
            string configName,
            string valueTypeName,
            string configNamespace,
            string dataNamespace,
            string keyType,
            List<ValueField> valueFields,
            bool openScript = false)
        {
            if (!Directory.Exists(configOutputPath)) Directory.CreateDirectory(configOutputPath);
            if (!Directory.Exists(dataOutputPath)) Directory.CreateDirectory(dataOutputPath);

            var dataCode = GenerateDataClassCode(valueTypeName, keyType, dataNamespace, valueFields);
            var dataFilePath = Path.Combine(dataOutputPath, $"{valueTypeName}.cs");
            File.WriteAllText(dataFilePath, dataCode, Encoding.UTF8);

            var configCode = GenerateConfigClassCode(configName, configNamespace, dataNamespace, keyType, valueTypeName);
            var configFilePath = Path.Combine(configOutputPath, $"{configName}.cs");
            File.WriteAllText(configFilePath, configCode, Encoding.UTF8);

            Debug.Log($"[ConfigCreator] 生成文件：\n{dataFilePath}\n{configFilePath}");

            if (openScript)
            {
                AssetDatabase.Refresh();
                var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataFilePath);
                var configAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(configFilePath);
                if (dataAsset != null) AssetDatabase.OpenAsset(dataAsset);
                if (configAsset != null) AssetDatabase.OpenAsset(configAsset);
            }
        }

        /// <summary>
        ///     判断类型名是否为数值类型
        /// </summary>
        public static bool IsNumericType(string type)
        {
            return type == "int" || type == "float" || type == "long" ||
                   type == "double" || type == "byte" || type == "short" ||
                   type == "uint" || type == "ulong" || type == "ushort";
        }

        #endregion
    }
}
