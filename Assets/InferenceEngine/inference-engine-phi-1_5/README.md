---
license: mit
library_name: unity-sentis
pipeline_tag: text-generation
tags:
  - unity-inference-engine
---

# Phi 1.5 in Unity 6 with Inference Engine

This is the [Microsoft Phi 1.5](https://huggingface.co/microsoft/phi-1_5) model running in Unity 6 with Inference Engine. Phi 1.5 is a Large Language Model trained on synthesized data. The model has 1.3 billion parameters.

## How to Use

* Create a new scene in Unity 6;
* Install `com.unity.ai.inference` from the package manager;
* Install `com.unity.nuget.newtonsoft-json` from the package manager;
* Add the `RunPhi15.cs` script to the Main Camera;
* Drag the `phi15.sentis` asset from the `models` folder into the `Model Asset` field;
* Drag the `vocab.json` asset from the `data` folder into the `Vocab Asset` field;
* Drag the `merges.txt` asset from the `data` folder into the `Merges Asset` field;

## Preview
Enter play mode. If working correctly the predicted text will be logged to the console.

## Inference Engine
Inference Engine is a neural network inference library for Unity. Find out more [here](https://docs.unity3d.com/Packages/com.unity.ai.inference@latest).

## Disclaimer
Like any LLM, this model has the possibility to generate undesirable or untruthful text. Use at your discretion.