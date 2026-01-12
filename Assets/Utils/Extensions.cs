using System.Collections.Generic;
using UnityEngine;

namespace LowoUN.Util {
    public static class Extensions {
        // MonoBehaviour类型的对象 的完全安全形态
        public static bool IsMonoValid (this MonoBehaviour monoObj) {
            return monoObj != null && monoObj.gameObject != null;
        }
        // MonoBehaviour类型的对象完全安全，且处于可视状态，才执行的逻辑（战斗中多为此类情况）
        public static bool IsValid (this MonoBehaviour monoObj) {
            return monoObj != null && monoObj.gameObject != null && monoObj.gameObject.activeInHierarchy;
        }
        public static bool IsValid (this string str) {
            return !string.IsNullOrEmpty (str);
        }
        public static bool IsNotValid (this string str) {
            return string.IsNullOrEmpty (str);
        }
        public static bool IsValid<T> (this List<T> lst) {
            return lst != null && lst.Count > 0;
        }
        public static bool IsNotValid<T> (this List<T> lst) {
            return lst == null || lst.Count <= 0;
        }
        public static bool IsNotValid<T, K> (this Dictionary<T, K> dict) {
            return dict == null || dict.Count <= 0;
        }
        public static bool IsValid<T, K> (this Dictionary<T, K> dict) {
            return dict != null && dict.Count > 0;
        }
    }
}