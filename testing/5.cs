using CommandSystem;
using Exiled.API.Features;
using Exiled.Events.Commands.PluginManager;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Next_generationSite_27.UnionP
{
    class u
    {
        static public IEnumerator<float> SearchForNumberFive(Player player)
        {
            // 等待一段时间确保UI完全加载
            yield return Timing.WaitForSeconds(2f);

            try
            {
                Log.Info("=== 开始搜索包含数字5的TMP UI元素 ===");
                FindNumberInTMPComponents();
                Log.Info("=== TMP搜索完成 ===");
            }
            catch (Exception ex)
            {
                Log.Info($"Error searching for number 5 in TMP: {ex.Message}");
            }
        }

        static public void FindNumberInTMPComponents()
        {
            try
            {
                // 查找所有包含"TMP"的组件（TextMeshPro, TextMeshProUGUI等）
                var allComponents = GameObject.FindObjectsOfType<Component>();
                var tmpComponents = allComponents.Where(c =>
                    c != null &&
                    (c.GetType().Name.Contains("TextMeshPro") ||
                     c.GetType().FullName.Contains("TMPro"))
                ).ToList();

                Log.Info($"找到 {tmpComponents.Count} 个TMP组件");

                foreach (var tmpComponent in tmpComponents)
                {
                    if (tmpComponent == null || tmpComponent.gameObject == null) continue;
                    BuildTreePath(tmpComponent.gameObject);
                    // 获取文本内容
                    string text = GetTMPText(tmpComponent);
                    if (true)
                    {
                        Log.Info($"找到包含数字5的TMP文本: '{text}'");

                        // 构建并输出树状路径
                        string treePath = BuildTreePath(tmpComponent.gameObject);
                        Log.Info($"树状路径: {treePath} -> \"{text}\"");
                        Log.Info("---");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Info($"TMP组件搜索错误: {ex.Message}");
            }
        }

        static public string GetTMPText(Component tmpComponent)
        {
            if (tmpComponent == null) return null;

            try
            {
                // 方法1: 通过属性获取文本
                var textProperty = tmpComponent.GetType().GetProperty("text");
                if (textProperty != null && textProperty.CanRead)
                {
                    return textProperty.GetValue(tmpComponent, null) as string;
                }

                // 方法2: 通过字段获取文本
                var textField = tmpComponent.GetType().GetField("m_text",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (textField != null)
                {
                    return textField.GetValue(tmpComponent) as string;
                }

                // 方法3: 尝试其他可能的文本字段
                var textFields = tmpComponent.GetType().GetFields(
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                    .Where(f => f.Name.ToLower().Contains("text") && f.FieldType == typeof(string))
                    .ToArray();

                foreach (var field in textFields)
                {
                    string value = field.GetValue(tmpComponent) as string;
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Info($"获取TMP文本时出错: {ex.Message}");
                return null;
            }
        }

        static public string BuildTreePath(GameObject targetObject)
        {
            List<string> pathComponents = new List<string>();
            GameObject current = targetObject;

            // 向上遍历到根对象
            int maxDepth = 20; // 防止无限循环
            int currentDepth = 0;

            while (current != null && current.transform != null && currentDepth < maxDepth)
            {
                string componentInfo = GetGameObjectInfo(current);
                pathComponents.Add(componentInfo);

                // 如果到达场景根对象或没有父对象则停止
                if (current.transform.parent == null)
                    break;

                current = current.transform.parent?.gameObject;
                currentDepth++;
            }

            // 反转列表以获得从根到目标的路径
            pathComponents.Reverse();

            return string.Join(" -> ", pathComponents);
        }

        static public string GetGameObjectInfo(GameObject obj)
        {
            if (obj == null) return "null";

            string info = obj.name;

            // 添加重要的组件信息
            Component[] components = obj.GetComponents<Component>();
            List<string> componentNames = new List<string>();

            foreach (Component component in components)
            {
                if (component == null) continue;

                string componentName = component.GetType().Name;

                // 特殊处理TMP组件
                if (componentName.Contains("TextMeshPro"))
                    componentNames.Add("TMP");
                else if (componentName.Contains("TMP"))
                    componentNames.Add(componentName.Replace("TextMeshPro", "TMP"));
                else if (component is Canvas)
                    componentNames.Add("Canvas");
                else if (component is UnityEngine.UI.Text)
                    componentNames.Add("Text");
                else if (component is UnityEngine.UI.Image)
                    componentNames.Add("Image");
                else if (component is UnityEngine.UI.Button)
                    componentNames.Add("Button");
            }

            if (componentNames.Count > 0)
            {
                info += $"[{string.Join(",", componentNames.Distinct())}]";
            }

            return info;
        }

        static public bool ContainsNumberFive(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            // 方法1: 直接查找字符'5'
            if (text.Contains("回合"))
                return true;
            if (text.Contains("round"))
                return true;

            // 方法2: 使用正则表达式查找数字5（包括5.0, 05等格式）
            return Regex.IsMatch(text, @"\b5\b") || Regex.IsMatch(text, @"[^0-9]5[^0-9]");
        }
    }
    }
