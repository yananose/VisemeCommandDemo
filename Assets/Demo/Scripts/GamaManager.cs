using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Omochaya;

public class GamaManager : MonoBehaviour
{
    // fields
    [SerializeField] private VisemeCommand command = null;
    [SerializeField] private Text question = null;
    [SerializeField] private Text information = null;
    [SerializeField] private TextAsset preset = null;
    [SerializeField, TextArea(1, 3)] private List<string> questions = new List<string> { "好きな果物は何ですか？" };
    private readonly string SAVE_KEY = "VisemeCommandDemo";
    private Scenario scenario = null;

    // Start is called before the first frame update
    void Start()
    {
        this.scenario = new Scenario(Scenario());
    }

    // Update is called once per frame
    void Update()
    {
        this.scenario.Update();
    }

    private void Load()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            var save = PlayerPrefs.GetString(SAVE_KEY);
            if (this.command.SetWordsFromJson(save))
            {
                return;
            }
            else
            {
                Debug.Log("ロード失敗..." + save);
            }
        }

        var preset = this.preset.text;
        if (this.command.SetWordsFromJson(preset))
        {
            this.command.SetWordsFromJson(preset);
            Save();
        }
        else
        {
            Debug.LogAssertion("preset.json が不正です..." + preset);
        }
    }

    private void Save()
    {
        var json = this.command.GetWordsToJson();
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    // scenario
    private IEnumerator<Func<bool>> Scenario()
    {
        Load();

        var yes = "そうです";
        var waitMesssage = "（合っていたら画面をタップするか、\n「" + yes + "」と言ってください）";
        var descriptionMesssage = "（受け付ける単語は\n";
        foreach(var word in this.command.GetWords())
        {
            if (word != yes)
            {
                descriptionMesssage += "　「" + word + "」\n";
            }
        }
        descriptionMesssage += "　です。単語で答えてください）";

        while (true)
        {
            var word = string.Empty;
            var selected = string.Empty;
            var viseme = new int[0];
            var check = 0;
            this.question.text = this.questions[UnityEngine.Random.Range(0, this.questions.Count)];
            this.information.text = string.Empty;

            this.command.SetWordWeight(yes, 0f);
            while (true)
            {
                yield return this.command.IsSampling;
                word = this.command.GetSelectedWord();
                if (!string.IsNullOrEmpty(word))
                {
                    this.question.text = word + "ですか？";
                    selected = word;
                    viseme = this.command.Visemes;
                    Debug.Log("0[" + word + "] visims:" + string.Join(", ", viseme));
                    break;
                }
                else
                {
                    this.information.text = descriptionMesssage;
                }
            }

            this.command.SetWordWeight(yes, 2f);
            this.information.text = waitMesssage;
            while (true)
            {
                while (true)
                {
                    yield return null;
                    if (Input.GetMouseButton(0) || 0 < Input.touchCount)
                    {
                        word = yes;
                        break;
                    }
                    if (this.command.Updated)
                    {
                        word = this.command.GetSelectedWord();
                        Debug.Log("1[" + word + "] visims:" + string.Join(", ", this.command.Visemes));
                        break;
                    }
                }

                if (word == yes)
                {
                    // 正解
                    if (2 < viseme.Length)
                    {
                        this.command.UpdateWord(selected, viseme);
                        Save();
                    }
                    this.question.text = "ありがとうございます。\nそれでは、";
                    this.information.text = string.Empty;
                    yield return new Wait(1.5f).Update;
                    break;
                }
                else if (word == selected)
                {
                    this.question.text = selected + "\nで合ってますよね？";
                    this.information.text = waitMesssage;
                }
                else if (string.IsNullOrEmpty(word))
                {
                    this.question.text = selected + "\nで合ってますか？";
                    this.information.text = descriptionMesssage;
                }
                else
                {
                    switch (check)
                    {
                        case 0:
                            this.question.text = "あ、" + word + "ですか？";
                            this.information.text = descriptionMesssage;
                            check = 1;
                            break;
                        case 1:
                            this.question.text = "やっぱり\n" + word + "ですか？";
                            this.information.text = descriptionMesssage;
                            check = 2;
                            break;
                        case 2:
                            this.question.text = "それとも\n" + word + "ですか？";
                            this.information.text = descriptionMesssage;
                            check = 1;
                            break;
                        default:
                            break;
                    }
                    selected = word;
                    viseme = this.command.Visemes;
                }
            }
        }
    }
}
