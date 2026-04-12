using UnityEngine;

namespace CFramework.Runtime.Extensions
{
    /// <summary>
    /// Transform 常用拓展方法
    /// </summary>
    public static class TransformExtensions
    {
        #region 重置

        /// <summary>
        /// 重置位置为零点
        /// </summary>
        public static Transofrm ResetPosition(this Transform transform)
        {
            transform.position = Vector3.zero;
        }

        /// <summary>
        /// 重置本地位置为零点
        /// </summary>
        public static Transofrm ResetLocalPosition(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// 重置旋转为默认值
        /// </summary>
        public static Transofrm ResetRotation(this Transform transform)
        {
            transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// 重置本地旋转为默认值
        /// </summary>
        public static Transofrm ResetLocalRotation(this Transform transform)
        {
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// 重置缩放为一
        /// </summary>
        public static Transofrm ResetScale(this Transform transform)
        {
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 重置所有属性（位置、旋转、缩放）
        /// </summary>
        public static Transofrm ResetAll(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        #endregion

        #region 位置

        /// <summary>
        /// 设置 X 坐标
        /// </summary>
        public static Transofrm WithX(this Transform transform, float x)
        {
            var pos = transform.position;
            pos.x = x;
            transform.position = pos;
        }

        /// <summary>
        /// 设置 Y 坐标
        /// </summary>
        public static Transofrm WithY(this Transform transform, float y)
        {
            var pos = transform.position;
            pos.y = y;
            transform.position = pos;
        }

        /// <summary>
        /// 设置 Z 坐标
        /// </summary>
        public static Transofrm WithZ(this Transform transform, float z)
        {
            var pos = transform.position;
            pos.z = z;
            transform.position = pos;
        }

        /// <summary>
        /// 设置本地 X 坐标
        /// </summary>
        public static Transofrm WithLocalX(this Transform transform, float x)
        {
            var pos = transform.localPosition;
            pos.x = x;
            transform.localPosition = pos;
        }

        /// <summary>
        /// 设置本地 Y 坐标
        /// </summary>
        public static Transofrm WithLocalY(this Transform transform, float y)
        {
            var pos = transform.localPosition;
            pos.y = y;
            transform.localPosition = pos;
        }

        /// <summary>
        /// 设置本地 Z 坐标
        /// </summary>
        public static Transofrm WithLocalZ(this Transform transform, float z)
        {
            var pos = transform.localPosition;
            pos.z = z;
            transform.localPosition = pos;
        }

        /// <summary>
        /// 仅设置位置 XY 分量，Z 不变
        /// </summary>
        public static Transofrm WithPositionXY(this Transform transform, float x, float y)
        {
            var pos = transform.position;
            pos.x = x;
            pos.y = y;
            transform.position = pos;
        }

        /// <summary>
        /// 仅设置位置 XZ 分量，Y 不变
        /// </summary>
        public static Transofrm WithPositionXZ(this Transform transform, float x, float z)
        {
            var pos = transform.position;
            pos.x = x;
            pos.z = z;
            transform.position = pos;
        }

        #endregion

        #region 旋转

        /// <summary>
        /// 设置欧拉角 X 分量
        /// </summary>
        public static Transofrm WithEulerX(this Transform transform, float x)
        {
            var euler = transform.eulerAngles;
            euler.x = x;
            transform.eulerAngles = euler;
        }

        /// <summary>
        /// 设置欧拉角 Y 分量
        /// </summary>
        public static Transofrm WithEulerY(this Transform transform, float y)
        {
            var euler = transform.eulerAngles;
            euler.y = y;
            transform.eulerAngles = euler;
        }

        /// <summary>
        /// 设置欧拉角 Z 分量
        /// </summary>
        public static Transofrm WithEulerZ(this Transform transform, float z)
        {
            var euler = transform.eulerAngles;
            euler.z = z;
            transform.eulerAngles = euler;
        }

        /// <summary>
        /// 设置本地欧拉角 X 分量
        /// </summary>
        public static Transofrm WithLocalEulerX(this Transform transform, float x)
        {
            var euler = transform.localEulerAngles;
            euler.x = x;
            transform.localEulerAngles = euler;
        }

        /// <summary>
        /// 设置本地欧拉角 Y 分量
        /// </summary>
        public static Transofrm WithLocalEulerY(this Transform transform, float y)
        {
            var euler = transform.localEulerAngles;
            euler.y = y;
            transform.localEulerAngles = euler;
        }

        /// <summary>
        /// 设置本地欧拉角 Z 分量
        /// </summary>
        public static Transofrm WithLocalEulerZ(this Transform transform, float z)
        {
            var euler = transform.localEulerAngles;
            euler.z = z;
            transform.localEulerAngles = euler;
        }

        #endregion

        #region 缩放

        /// <summary>
        /// 设置缩放 X 分量
        /// </summary>
        public static Transofrm WithScaleX(this Transform transform, float x)
        {
            var scale = transform.localScale;
            scale.x = x;
            transform.localScale = scale;
        }

        /// <summary>
        /// 设置缩放 Y 分量
        /// </summary>
        public static Transofrm WithScaleY(this Transform transform, float y)
        {
            var scale = transform.localScale;
            scale.y = y;
            transform.localScale = scale;
        }

        /// <summary>
        /// 设置缩放 Z 分量
        /// </summary>
        public static Transofrm WithScaleZ(this Transform transform, float z)
        {
            var scale = transform.localScale;
            scale.z = z;
            transform.localScale = scale;
        }

        /// <summary>
        /// 统一设置缩放值
        /// </summary>
        public static Transofrm WithUniformScale(this Transform transform, float scale)
        {
            transform.localScale = Vector3.one * scale;
        }

        #endregion

        #region 层级操作

        /// <summary>
        /// 销毁所有子物体
        /// </summary>
        public static Transofrm DestroyAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 立即销毁所有子物体（编辑器模式使用）
        /// </summary>
        public static Transofrm DestroyImmediateAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 获取或创建指定名称的子物体
        /// </summary>
        public static Transform FindOrCreateChild(this Transform transform, string name)
        {
            var child = transform.Find(name);
            if (child != null)
            {
                return child;
            }

            var go = new GameObject(name);
            go.transform.WithParent(transform, false);
            return go.transform;
        }

        /// <summary>
        /// 遍历所有子物体执行操作
        /// </summary>
        public static Transofrm ForEachChild(this Transform transform, System.Action<Transform> action)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                action.Invoke(transform.GetChild(i));
            }
        }

        /// <summary>
        /// 获取子物体数量
        /// </summary>
        public static int ChildCount(this Transform transform)
        {
            return transform.childCount;
        }

        /// <summary>
        /// 获取最后一个子物体
        /// </summary>
        public static Transform GetLastChild(this Transform transform)
        {
            return transform.childCount > 0
                ? transform.GetChild(transform.childCount - 1)
                : null;
        }

        #endregion

        #region 查找

        /// <summary>
        /// 查找最近的双亲中指定类型的组件
        /// </summary>
        public static T GetComponentInParent<T>(this Transform transform) where T : Component
        {
            return transform.GetComponentInParent<T>();
        }

        /// <summary>
        /// 查找或添加组件
        /// </summary>
        public static T GetOrAddComponent<T>(this Transform transform) where T : Component
        {
            return transform.GetComponent<T>() ?? transform.gameObject.AddComponent<T>();
        }

        #endregion
    }
}
