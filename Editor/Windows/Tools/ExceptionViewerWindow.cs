using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Windows.Tools
{
    /// <summary>
    ///     全局异常查看器窗口
    /// </summary>
    public sealed class ExceptionViewerWindow : EditorWindow
    {
        private readonly List<ExceptionInfo> _exceptions = new();
        private Vector2 _scrollPosition;

        private void OnEnable()
        {
            // 注册异常处理
            if (Application.isPlaying)
            {
                var scope = GameScope.Instance;
                if (scope != null && scope.Container != null)
                {
                    var dispatcher = scope.Container.Resolve(typeof(IExceptionDispatcher)) as IExceptionDispatcher;
                    dispatcher?.RegisterHandler(OnException);
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // 工具栏
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton)) _exceptions.Clear();

                GUILayout.FlexibleSpace();

                EditorGUILayout.LabelField($"Count: {_exceptions.Count}");
            }

            EditorGUILayout.Space(5);

            // 异常列表
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (var i = _exceptions.Count - 1; i >= 0; i--) DrawExceptionItem(_exceptions[i]);

            EditorGUILayout.EndScrollView();
        }

        [MenuItem("CFramework/Exception Viewer")]
        public static void ShowWindow()
        {
            GetWindow<ExceptionViewerWindow>("Exception Viewer");
        }

        private void OnException(Exception ex)
        {
            _exceptions.Add(new ExceptionInfo
            {
                Time = DateTime.Now,
                Message = ex.Message,
                StackTrace = ex.StackTrace
            });

            Repaint();
        }

        private void DrawExceptionItem(ExceptionInfo info)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"[{info.Time:HH:mm:ss}] {info.Message}", EditorStyles.boldLabel);

            if (GUILayout.Button("Copy Stack Trace", EditorStyles.linkLabel))
                EditorGUIUtility.systemCopyBuffer = info.StackTrace;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private struct ExceptionInfo
        {
            public DateTime Time;
            public string Message;
            public string StackTrace;
        }
    }
}