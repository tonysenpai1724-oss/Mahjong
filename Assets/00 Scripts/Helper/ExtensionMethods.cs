using DG.Tweening;
using DG.Tweening.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace Gosu.ExtensionMethods
{
    public static class ExtensionMethods
    {
        public static IEnumerator IE_DelayedCall(float delay, Action callback)
        {
            yield return Wait(delay);
            callback?.Invoke();
        }
        
        public static int RandomSystem(List<float[]> lstRate)
        {
            //Get %
            for (int i = 1; i < lstRate.Count; i++)
            {
                lstRate[i][1] = lstRate[i - 1][1] + lstRate[i][1];
            }

            float maxRate = lstRate[lstRate.Count - 1][1];
            float rate = UnityEngine.Random.Range(0, maxRate);
            int number;
            float newRate;
            for (int i = 0; i < lstRate.Count; i++)
            {
                newRate = lstRate[i][1];
                if (rate < newRate)
                {
                    number = (int)lstRate[i][0];
                    return number;
                }
            }

            number = (int)lstRate[0][0];
            return number;
        }

        public static string FormatTimer(int secs)
        {
            int hours = secs / 3600;
            int minutes = (secs - hours * 3600) / 60;
            int seconds = secs - hours * 3600 - minutes * 60;

            if (hours == 0)
            {
                return $"{minutes:00}:{seconds:00}";
            }

            return $"{hours:00}:{minutes:00}:{seconds:00}";
        }

        public static string FormatTimerChar(int secs)
        {
            int hours = secs / 3600;
            int minutes = (secs - hours * 3600) / 60;
            int seconds = secs - hours * 3600 - minutes * 60;

            if (hours == 0)
            {
                return $"{minutes:00}m{seconds:00}s";
            }

            return $"{hours:00}h{minutes:00}m{seconds:00}s";
        }

        public static string FormatTimerChar_ToMinutes(int secs)
        {
            int hours = secs / 3600;
            int minutes = (secs - hours * 3600) / 60;

            if (hours == 0)
            {
                return $"{minutes:00}m";
            }

            return $"{hours:00}h{minutes:00}m";
        }

        public static bool RandomResult_100Percent(int rate)
        {
            int rd = UnityEngine.Random.Range(0, 100);
            if (rd <= rate)
            {
                return true;
            }

            return false;
        }

        public static bool RandomResult_1000(int rate)
        {
            int rd = UnityEngine.Random.Range(0, 1000);
            if (rd <= rate)
            {
                return true;
            }

            return false;
        }

        //Shuffle
        private static readonly System.Random Rng = new();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        //IEnumerator
        private static readonly Dictionary<float, WaitForSeconds> DicWait = new();

        public static WaitForSeconds Wait(float time)
        {
            DicWait.TryGetValue(time, out var waitForSeconds);
            if (waitForSeconds == null)
            {
                waitForSeconds = new WaitForSeconds(time);
                DicWait.Add(time, waitForSeconds);
            }

            return waitForSeconds;
        }

        //IEnumerator real time
        private static readonly Dictionary<float, WaitForSecondsRealtime> DicWaitRealTime = new();

        public static WaitForSecondsRealtime WaitRealTime(float time)
        {
            DicWaitRealTime.TryGetValue(time, out var waitForSecondsRealtime);
            if (waitForSecondsRealtime == null)
            {
                waitForSecondsRealtime = new WaitForSecondsRealtime(time);
                DicWaitRealTime.Add(time, waitForSecondsRealtime);
            }

            return waitForSecondsRealtime;
        }

        public static Vector2 GetRandomPos(float innerRadius, float outerRadius)
        {
            return UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(innerRadius, outerRadius);
        }

        public static Vector3 GetRandomPos3D(float innerRadius, float outerRadius)
        {
            return UnityEngine.Random.insideUnitSphere.normalized * UnityEngine.Random.Range(innerRadius, outerRadius);
        }

        public static string RemoveSpecialCharacter(string myString)
        {
            var newChars = myString.Select(ch =>
                    (ch is >= 'a' and <= 'z'
                     || ch is >= 'A' and <= 'Z'
                     || ch is >= '0' and <= '9'
                     || ch == '-')
                        ? ch
                        : '-')
                .ToArray();

            return new string(newChars);
        }

        public static Tweener DoText(this TextMeshProUGUI label, string context, float speed = 0.02f,
            bool ignoreTimeScale = true)
        {
            if (label == null || string.IsNullOrEmpty(context))
            {
                return null;
            }

            float duration = context.Length * speed;
            label.text = "";
            int index = 0;

            int Getter()
            {
                return index;
            }

            void Setter(int x)
            {
                index = x;
                label.text = context.Substring(0, x);
            }

            return DOTween.To(Getter, Setter, context.Length, duration).SetUpdate(ignoreTimeScale);
        }

        public static int GetSecondFromTime(DateTime preTime, DateTime curTime)
        {
            TimeSpan timeSpan = curTime - preTime;
            return (timeSpan.Days * 86400) + (timeSpan.Hours * 3600) + (timeSpan.Minutes * 60) + timeSpan.Seconds;
        }

        public static int GetSecondFromTimeSpan(TimeSpan timeSpan)
        {
            return (timeSpan.Days * 86400) + (timeSpan.Hours * 3600) + (timeSpan.Minutes * 60) + timeSpan.Seconds;
        }

        public static TimeSpan GetTimeSpanFromSecond(int second)
        {
            int day = second / 86400;
            second %= 86400;

            int hour = second / 3600;
            second %= 3600;

            int minute = second / 60;
            second %= 60;

            return new TimeSpan(day, hour, minute, second);
        }

        //Get day calendar
        public static int GetStartDayInWeek(int currentDay, int currentDayInWeek)
        {
            int maxDayInWeek = 6;
            for (int day = currentDay; day > 1; day--)
            {
                currentDayInWeek--;
                if (currentDayInWeek < 0)
                {
                    currentDayInWeek = maxDayInWeek;
                }
            }

            return currentDayInWeek;
        }

        //Rotate
        public static Quaternion GetQuaternionFromDirection(Vector3 positionA, Vector3 positionB)
        {
            Quaternion rotation = Quaternion.LookRotation(positionB - positionA);
            return rotation;
        }

        // Get Time
        public static int GetTheRestOfTheDay(DateTime curTime)
        {
            return 86400 - ((curTime.Hour * 3600) + (curTime.Minute * 60) + curTime.Second);
        }

        //Load Texture from Web

        public static async Task<Texture2D> LoadTextureFromWeb(string url)
        {
            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            // begin request:
            var asyncOp = request.SendWebRequest();

            // await until it's done: 
            while (asyncOp.isDone == false)
                await Task.Delay(1000 / 30); //30 hertz

            // read results:
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log($"{request.error}, URL:{request.url}");
                return null;
            }

            // return valid results:
            return DownloadHandlerTexture.GetContent(request);
        }

        //GetTextureRead
        public static Texture2D GetTextureReadable(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }


        //GetLinkedList
        private static readonly LinkedList<int> Stack = new();
        //public static LinkedList<int> SplitNumber(int number)
        //{
        //    stack.Clear();
        //    while (number > 0)
        //    {
        //        stack.AddFirst(number % 10);
        //        number = number / 10;
        //    }

        //    return stack;
        //}
        public static LinkedList<int> SplitNumber2(int number)
        {
            Stack.Clear();
            while (number > 0)
            {
                Stack.AddFirst(number % 100);
                number = number / 100;
            }

            return Stack;
        }

        public static void GemDetail(int gem, out int type, out int status, out int right, out int bottom)
        {
            type = gem / 1000;
            var temp = gem % 1000;
            status = temp / 100;
            var temp2 = temp % 100;
            right = temp2 / 10;
            bottom = temp2 % 10;
        }

        //Clear Log
        public static void ClearLog()
        {
#if UNITY_EDITOR
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
#endif
        }

        //Get all transform
        public static List<Transform> GetAllTransforms(Transform parent)
        {
            var transformList = new List<Transform>();
            BuildTransformList(transformList, parent);
            return transformList;
        }

        private static void BuildTransformList(ICollection<Transform> transforms, Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            foreach (Transform t in parent)
            {
                transforms.Add(t);
                BuildTransformList(transforms, t);
            }
        }

        public static Texture2D ToTexture2D(Texture texture)
        {
            Texture2D dest = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);

            // Graphics.CopyTexture(renderTexture, dest);

            return dest;
        }
    }
}