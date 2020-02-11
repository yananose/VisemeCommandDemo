// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VisemeCommand.cs" company="yoshikazu yananose">
//   (c) 2019 machi no omochaya-san.
// </copyright>
// <summary>
//   The tester.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Omochaya
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 音声による操作
    /// </summary>
    public class VisemeCommand : MonoBehaviour
    {
        // fields (readonly)
        [SerializeField] private OVRLipSyncContext lipSyncContext = null;
        [SerializeField] private float strictness = 0.5f;
        [SerializeField] private int indecisiveCount = 4;
        [SerializeField] private int visemesMin = 2;
        [SerializeField] private int visemesMax = 10;

        static bool IsNullOrEmpty(string a)
        {
            return string.IsNullOrEmpty(a);
        }

        static bool IsNullOrEmpty<T0, T1>(T0 a) where T0 : ICollection<T1>
        {
            if (0 < a?.Count)
            {
                return false;
            }
            return true;
        }

        static bool IsNullOrEmpty<T0>(T0[] a)
        {
            if (0 < a?.Length)
            {
                return false;
            }
            return true;
        }

        // inner classes
        [System.Serializable]
        private struct JsonData
        {
            // inner classes
            [System.Serializable]
            public struct Element
            {
                // inner classes
                [System.Serializable]
                public struct Info
                {
                    // fields
                    public int[] visemes;
                    // methods
                    public void Setup(int[] visemes)
                    {
                        this.visemes = visemes;
                    }
                    public bool IsValid()
                    {
                        if (IsNullOrEmpty(this.visemes))
                        {
                            return false;
                        }

                        return true;
                    }
                }
                // fields
                public string word;
                public Info[] indecisive;
                // methods
                public void Setup(Data data)
                {
                    this.word = data.word;
                    this.indecisive = data.indecisive.ToArray();
                }
                public bool IsValid()
                {
                    if (IsNullOrEmpty(this.word))
                    {
                        return false;
                    }

                    if (IsNullOrEmpty(this.indecisive))
                    {
                        return false;
                    }

                    foreach (var info in this.indecisive)
                    {
                        if (info.IsValid() == false)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            // fields
            public Element[] array;
            // methods
            public void Setup(List<Data> data)
            {
                var count = data.Count;
                this.array = new JsonData.Element[count];
                for (var i = 0; i < count; i++)
                {
                    this.array[i].Setup(data[i]);
                }
            }
            public bool IsValid()
            {
                if (IsNullOrEmpty(this.array))
                {
                    return false;
                }

                foreach (var element in this.array)
                {
                    if (element.IsValid() == false)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
        private struct Data
        {
            // fields
            public string word;
            public float average;
            public List<JsonData.Element.Info> indecisive;
            public bool exclude;
            // methods
            public void Setup(JsonData.Element element)
            {
                this.word = element.word;
                if (element.indecisive != null)
                {
                    this.indecisive = new List<JsonData.Element.Info>(element.indecisive);
                }
                else
                {
                    this.indecisive = new List<JsonData.Element.Info>();
                }
                this.average = GetAverage();
                this.exclude = false;
            }
            public void Setup(string word)
            {
                this.word = word;
                this.indecisive = new List<JsonData.Element.Info>(0);
                this.average = 0f;
                this.exclude = false;
            }
            public void Setup(string word, int[] visemes)
            {
                Setup(word);
                var info = new JsonData.Element.Info();
                info.Setup(visemes);
                this.average = 1f;
                this.indecisive.Add(info);
            }
            public float GetScore(int[] visemes)
            {
                var ret = 0f;
                if (0 < this.indecisive.Count)
                {
                    foreach (var info in this.indecisive)
                    {
                        ret += GetMatchRate(visemes, info.visemes);
                    }
                    ret /= this.indecisive.Count;
                }
                return ret;
            }
            public float GetAverage()
            {
                var ret = 0f;
                if (0 < this.indecisive.Count)
                {
                    foreach (var info in this.indecisive)
                    {
                        ret += GetScore(info.visemes);
                    }
                    ret /= this.indecisive.Count;
                }
                return ret;
            }
        }
        private struct Match
        {
            // fields
            private int hit;
            private int count;

            // properties
            public float Rate { get { return 0 < this.count ? (float)this.hit / this.count : 0f; } }

            // methods
            public void Hit(int count)
            {
                this.hit += count;
                this.count += count;
            }
            public void Miss(int count)
            {
                this.count += count;
            }
        }

        // fields
        private List<int> buffer = new List<int>();
        private bool updated;
        private int[] visemes = new int[0];
        private List<Data> data = new List<Data>();

        /// <summary>
        /// データが更新されたか？
        /// </summary>
        public bool Updated { get { return this.updated; } }

        /// <summary>
        /// サンプリングしたデータ
        /// </summary>
        public int[] Visemes { get { return this.visemes; } }

        /// <summary>
        /// 判定の厳密さ
        /// </summary>
        public float Strictness { get { return this.strictness; } set { this.strictness = value; } }

        /// <summary>
        /// サンプリング中？
        /// </summary>
        public bool IsSampling()
        {
            return !this.updated;
        }

        /// <summary>
        /// 設定されている単語と音声情報を初期化
        /// </summary>
        public void InitWords()
        {
            SetWords(null);
        }

        /// <summary>
        /// 設定されている単語の数を取得
        /// </summary>
        /// <returns>単語の数</returns>
        public int GetWordsCount()
        {
            return this.data.Count;
        }

        /// <summary>
        /// 単語をまとめて設定し音声情報を初期化
        /// </summary>
        /// <param name="words">単語群</param>
        public void SetWords(string[] words)
        {
            this.data.Clear();
            if (!IsNullOrEmpty(words))
            {
                var count = words.Length;
                for (var i = 0; i < count; i++)
                {
                    var data = new Data();
                    data.Setup(words[i]);
                    this.data.Add(data);
                }
                this.visemes = new int[0];
            }
        }

        /// <summary>
        /// 設定されている単語をまとめて取得
        /// </summary>
        /// <returns>単語群</returns>
        public string[] GetWords()
        {
            var count = GetWordsCount();
            var ret = new string[count];
            for (var i = 0; i < count; i++)
            {
                ret[i] = this.data[i].word;
            }
            return ret;
        }

        /// <summary>
        /// 設定されている単語を取得
        /// </summary>
        /// <param name="index">単語のインデックス</param>
        /// <returns>単語</returns>
        public string GetWord(int index)
        {
            if (index < 0 || GetWordsCount() <= index)
            {
                return string.Empty;
            }
            return this.data[index].word;
        }

        /// <summary>
        /// 単語のインデックスを取得
        /// </summary>
        /// <param name="word">単語</param>
        /// <returns>単語のインデックス</returns>
        public int GetWordIndex(string word)
        {
            var ret = this.data.FindIndex(a => a.word == word);
            return ret;
        }

        /// <summary>
        /// 設定されている単語とその音声情報をまとめて json で取得
        /// </summary>
        /// <returns>json</returns>
        public string GetWordsToJson()
        {
            var count = GetWordsCount();
            var jsonData = new JsonData();
            jsonData.Setup(this.data);
            var ret = JsonUtility.ToJson(jsonData);
            return ret;
        }

        /// <summary>
        /// 単語とその音声情報を json で設定
        /// </summary>
        /// <param name="json">json</param>
        /// <returns>設定できたか？</returns>
        public bool SetWordsFromJson(string json)
        {
            var jsonData = JsonUtility.FromJson<JsonData>(json);
            if (jsonData.IsValid() == false)
            {
                return false;
            }

            InitWords();
            foreach (var element in jsonData.array)
            {
                var data = new Data();
                data.Setup(element);
                this.data.Add(data);
            }

            return true;
        }

        /// <summary>
        /// 単語の音声情報をサンプリングしたデータで更新
        /// 指定された単語がなければ追加
        /// </summary>
        /// <param name="word">単語</param>
        /// <returns>追加 or 更新できたか？</returns>
        public bool UpdateWord(string word)
        {
            return UpdateWord(word, this.visemes);
        }

        /// <summary>
        /// 単語の音声情報を更新
        /// 指定された単語がなければ追加
        /// </summary>
        /// <param name="word">単語</param>
        /// <param name="visemes">音声情報(サンプリングしたデータ)</param>
        /// <returns>追加 or 更新できたか？</returns>
        public bool UpdateWord(string word, int[] visemes)
        {
            if (IsNullOrEmpty(word))
            {
                return false;
            }

            var index = GetWordIndex(word);
            if (index < 0)
            {
                var data = new Data();
                data.Setup(word, visemes);
                this.data.Add(data);
                return true;
            }

            return UpdateWord(index, visemes);
        }

        /// <summary>
        /// 単語の音声情報をサンプリングしたデータで更新
        /// </summary>
        /// <param name="index">単語のインデックス</param>
        /// <returns>更新できたか？</returns>
        public bool UpdateWord(int index)
        {
            return UpdateWord(index, this.visemes);
        }

        /// <summary>
        /// 単語の音声情報を更新
        /// </summary>
        /// <param name="index">単語のインデックス</param>
        /// <param name="visemes">音声情報(サンプリングしたデータ)</param>
        /// <returns>更新できたか？</returns>
        public bool UpdateWord(int index, int[] visemes)
        {
            if (index < 0 || GetWordsCount() <= index)
            {
                return false;
            }

            if (IsNullOrEmpty(visemes) || visemes.Length < this.visemesMin || this.visemesMax < visemes.Length)
            {
                return false;
            }

            var data = this.data[index];
            var indecisive = this.data[index].indecisive;

            // remove old
            if (this.indecisiveCount <= indecisive.Count)
            {
                indecisive.RemoveRange(0, 1 + indecisive.Count - this.indecisiveCount);
            }

            // add
            var info = new JsonData.Element.Info();
            info.Setup(visemes);
            indecisive.Add(info);
            data.average = data.GetAverage();

            return true;
        }

        /// <summary>
        /// 音声で選択された単語を取得
        /// </summary>
        /// <returns>選択された単語</returns>
        public string GetSelectedWord()
        {
            return GetSelectedWord(this.visemes);
        }

        /// <summary>
        /// 音声で選択された単語を取得
        /// </summary>
        /// <returns>選択された単語のインデックス</returns>
        public int GetSelectedWordIndex()
        {
            return GetSelectedWordIndex(this.visemes);
        }

        /// <summary>
        /// 音声で選択された単語を取得
        /// </summary>
        /// <param name="visemes">音声情報(サンプリングしたデータ)</param>
        /// <returns>選択された単語</returns>
        public string GetSelectedWord(int[] visemes)
        {
            int index = GetSelectedWordIndex(visemes);
            return GetWord(index);
        }

        /// <summary>
        /// 音声で選択された単語を取得
        /// </summary>
        /// <param name="visemes">音声情報(サンプリングしたデータ)</param>
        /// <returns>選択された単語のインデックス</returns>
        public int GetSelectedWordIndex(int[] visemes)
        {
            if (IsNullOrEmpty(visemes) || visemes.Length < this.visemesMin || this.visemesMax < visemes.Length)
            {
                return -1;
            }

            var count = GetWordsCount();
            if (count <= 0)
            {
                return -1;
            }

            int ret = 0;
            var best = 0f;
            for (var i = 0; i < count; i++)
            {
                var data = this.data[i];
                if (!data.exclude)
                {
                    var score = data.GetScore(visemes); ;
                    if (best < score)
                    {
                        best = score;
                        ret = i;
                    }
                }
            }

            if (best < this.data[ret].average * this.strictness)
            {
                return -1;
            }

            return ret;
        }

        /// <summary>
        /// 単語を選択肢から除外するか設定
        /// </summary>
        /// <param name="word">単語</param>
        /// <param name="exclude">除外するか？</param>
        /// <returns>設定できたか？</returns>
        public bool SetWordExclude(string word, bool exclude)
        {
            var index = GetWordIndex(word);
            return SetWordExclude(index, exclude);
        }

        /// <summary>
        /// 単語を選択肢から除外するか設定
        /// </summary>
        /// <param name="index">単語のインデックス</param>
        /// <param name="exclude">除外するか？</param>
        /// <returns>設定できたか？</returns>
        public bool SetWordExclude(int index, bool exclude)
        {
            if (index < 0 || GetWordsCount() <= index)
            {
                return false;
            }
            var data = this.data[index];
            data.exclude = exclude;
            return true;
        }

        /// <summary>
        /// 単語が選択肢から除外されているか取得
        /// </summary>
        /// <param name="word">単語</param>
        /// <returns>除外されているか？</returns>
        public bool IsWordExclude(string word)
        {
            var index = GetWordIndex(word);
            return IsWordExclude(index);
        }

        /// <summary>
        /// 単語が選択肢から除外されているか取得
        /// </summary>
        /// <param name="index">単語のインデックス</param>
        /// <returns>除外されているか？</returns>
        public bool IsWordExclude(int index)
        {
            if (index < 0 || GetWordsCount() <= index)
            {
                return true;
            }
            return this.data[index].exclude;
        }

        /// <summary>
        /// 2つの int 配列の一致率を取得
        /// </summary>
        /// <param name="a">配列 A</param>
        /// <param name="b">配列 B</param>
        /// <returns>一致率(0.0 ～ 1.0)</returns>
        public static float GetMatchRate(int[] a, int[] b)
        {
            if (IsNullOrEmpty(a) || IsNullOrEmpty(b))
            {
                return 0f;
            }
            var ret = GetMatch(a, 0, b, 0).Rate;
            return ret;
        }
        private static Match GetMatch(int[] data, int dataIndex, int[] other, int otherFrom)
        {
            var ret = new Match();
            var dataRest = data.Length - dataIndex;
            var otherRest = other.Length - otherFrom;
            // 全部失敗した場合を初期値とする
            ret.Miss(dataRest + otherRest);
            if (0 < dataRest)
            {
                // data のひとつと残りの other を比較
                for (var i = 0; i < otherRest; i++)
                {
                    var otherIndex = otherFrom + i;
                    Match match;
                    if (data[dataIndex] == other[otherIndex])
                    {
                        // ここより後の成績を取得
                        match = GetMatch(data, dataIndex + 1, other, otherIndex + 1);
                        // ここより前の other を失敗とカウント
                        match.Miss(i);
                        // data を(other とまとめて)成功とカウント
                        match.Hit(1);
                    }
                    else
                    {
                        // ここより後の成績を取得（other はここも含める）
                        match = GetMatch(data, dataIndex + 1, other, otherIndex);
                        // ここより前の other を失敗とカウント
                        match.Miss(i);
                        // data を失敗とカウント
                        match.Miss(1);
                    }
                    // 成績がよければ更新
                    if (ret.Rate < match.Rate)
                    {
                        ret = match;
                    }
                }
            }
            return ret;
        }

        // Start is called before the first frame update
        void Start()
        {
            this.buffer.Clear();
            this.updated = false;
            InitWords();
        }

        // Update is called once per frame
        void Update()
        {
            this.updated = false;

            var raw = this.lipSyncContext.GetCurrentPhonemeFrame().Visemes;
            var best = 0;
            if (!IsNullOrEmpty(raw))
            {
                for (var i = 0; i < raw.Length; i++)
                {
                    if (raw[best] < raw[i])
                    {
                        best = i;
                    }
                }
            }

            var count = this.buffer.Count;
            if (best != 0)
            {
                if (count == 0 || this.buffer[count - 1] != best)
                {
                    if (count <= this.visemesMax)
                    {
                        this.buffer.Add(best);
                    }
                }
            }
            else if (0 < count)
            {
                if (this.visemesMin <= count && count <= this.visemesMax)
                {
                    this.updated = true;
                    this.visemes = this.buffer.ToArray();
                }
                this.buffer.Clear();
            }
        }
    }
}
