---
license: cc-by-4.0
library_name: unity-sentis
tags:
  - unity-inference-engine
---

# Jets in Unity 6 using Inference Engine

This is the [Jets](https://huggingface.co/imdanboy/jets) model running in Unity 6 with Inference Engine. It text-to-speech model that takes phonemes as an input and outputs wav data of a voice speaking the text.

## How to Use

* Create a new scene in Unity 6;
* Install `com.unity.ai.inference` from the package manager;
* Add the `RunJets.cs` script to the Main Camera;
* Add an AudioSource component to the Main Camera;
* Drag the `jets-text-to-speech.onnx` file from the `models` folder into the `Model Asset` field;
* Drag the `phoneme_dict.txt` file from the `data` folder into the `Phoneme Asset` field;

## Preview
Enter play mode. If working correctly you should hear the inferred audio of the voice.

## Inference Engine
Inference Engine is a neural network inference library for Unity. Find out more [here](https://docs.unity3d.com/Packages/com.unity.ai.inference@latest).

## License
Attribution for the original creators is required. See[Jets](https://huggingface.co/imdanboy/jets) for more details.

You must retain the copyright notice in the `phoneme_dict.txt` file.