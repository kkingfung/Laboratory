using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Unity.InferenceEngine;
using UnityEngine;

public class RunPhi15 : MonoBehaviour
{
    public ModelAsset modelAsset;
    public TextAsset vocabAsset;
    public TextAsset mergesAsset;

    const BackendType backend = BackendType.GPUCompute;

    //string outputString = "Once upon a time, there were three bears";
    string outputString = "One day an alien came down from Mars. It saw a chicken";

    // This is how many tokens you want. It can be adjusted.
    const int maxTokens = 100;

    //Make this smaller for more randomness
    const float predictability = 5f;

    //Special tokens
    const int END_OF_TEXT = 50256;

    //Store the vocabulary
    string[] tokens;

    Worker engine;

    int currentToken;
    int[] outputTokens = new int[maxTokens];

    // Used for special character decoding
    int[] whiteSpaceCharacters = new int[256];
    int[] encodedCharacters = new int[256];

    bool runInference;

    //stop after this many tokens
    const int stopAfter = 100;

    int totalTokens;

    string[] merges;
    Dictionary<string, int> vocab;

    void Start()
    {
        SetupWhiteSpaceShifts();

        LoadVocabulary();

        var model1 = ModelLoader.Load(modelAsset);
        //Create a new model to select the random token:

        var graph = new FunctionalGraph();
        var input = graph.AddInput(model1, 0);
        var currentTokenInput = graph.AddInput<int>(new TensorShape(), "currentToken");
        var row = Functional.Select(Functional.Forward(model1, input)[^1], 1, currentTokenInput);
        var output = Functional.Multinomial(predictability * row, 1);
        var model2 = graph.Compile(output);

        engine = new Worker(model2, backend);

        DecodePrompt(outputString);

        runInference = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (runInference)
        {
            RunInference();
        }
    }

    void RunInference()
    {
        using var tokensSoFar = new Tensor<int>(new TensorShape(1, maxTokens), outputTokens);
        using var index = new Tensor<int>(new TensorShape(), new[] { currentToken });

        engine.Schedule(tokensSoFar, index);

        using var probs = (engine.PeekOutput() as Tensor<int>).ReadbackAndClone();

        int ID = probs[0];

        //shift window down if got to the end
        if (currentToken >= maxTokens - 1)
        {
            for (int i = 0; i < maxTokens - 1; i++) outputTokens[i] = outputTokens[i + 1];
            currentToken--;
        }

        outputTokens[++currentToken] = ID;
        totalTokens++;

        if (ID == END_OF_TEXT || totalTokens >= stopAfter)
        {
            runInference = false;
        }
        else if (ID < 0 || ID >= tokens.Length)
        {
            // Really we should use the added_tokens.json for this
            outputString += " ";
        }
        else outputString += GetUnicodeText(tokens[ID]);

        Debug.Log(outputString);
    }

    void DecodePrompt(string text)
    {
        var inputTokens = GetTokens(text);

        for (int i = 0; i < inputTokens.Count; i++)
        {
            outputTokens[i] = inputTokens[i];
        }
        currentToken = inputTokens.Count - 1;
    }

    void LoadVocabulary()
    {
        var jsonText = vocabAsset.text;
        vocab = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonText);
        tokens = new string[vocab.Count];
        foreach (var item in vocab)
        {
            tokens[item.Value] = item.Key;
        }

        merges = mergesAsset.text.Split("\r\n");
    }

    // Translates encoded special characters to Unicode
    string GetUnicodeText(string text)
    {
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
        return Encoding.UTF8.GetString(bytes);
    }
    string GetASCIIText(string newText)
    {
        var bytes = Encoding.UTF8.GetBytes(newText);
        return ShiftCharacterUp(Encoding.GetEncoding("ISO-8859-1").GetString(bytes));
    }

    string ShiftCharacterDown(string text)
    {
        string outText = "";
        foreach (char letter in text)
        {
            outText += (letter <= 256) ? letter : (char)whiteSpaceCharacters[letter - 256];
        }
        return outText;
    }

    string ShiftCharacterUp(string text)
    {
        string outText = "";
        foreach (char letter in text)
        {
            outText += (char)encodedCharacters[letter];
        }
        return outText;
    }

    void SetupWhiteSpaceShifts()
    {
        for (int i = 0, n = 0; i < 256; i++)
        {
            encodedCharacters[i] = i;
            if (IsWhiteSpace(i))
            {
                encodedCharacters[i] = n + 256;
                whiteSpaceCharacters[n++] = i;
            }
        }
    }

    bool IsWhiteSpace(int i)
    {
        //returns true if it is a whitespace character
        return i <= 32 || (i >= 127 && i <= 160) || i == 173;
    }

    List<int> GetTokens(string text)
    {
        text = GetASCIIText(text);

        // Start with a list of single characters
        var inputTokens = new List<string>();
        foreach (var letter in text)
        {
            inputTokens.Add(letter.ToString());
        }

        ApplyMerges(inputTokens);

        //Find the ids of the words in the vocab
        var ids = new List<int>();
        foreach (var token in inputTokens)
        {
            if (vocab.TryGetValue(token, out int id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    void ApplyMerges(List<string> inputTokens)
    {
        foreach (var merge in merges)
        {
            string[] pair = merge.Split(' ');
            int n = 0;
            while (n >= 0)
            {
                n = inputTokens.IndexOf(pair[0], n);
                if (n != -1 && n < inputTokens.Count - 1 && inputTokens[n + 1] == pair[1])
                {
                    inputTokens[n] += inputTokens[n + 1];
                    inputTokens.RemoveAt(n + 1);
                }
                if (n != -1) n++;
            }
        }
    }

    void OnDestroy()
    {
        engine?.Dispose();
    }
}
