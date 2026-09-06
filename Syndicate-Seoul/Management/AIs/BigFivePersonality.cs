using System;
using UnityEngine;

[Serializable]
public class BigFivePersonality
{
    [Range(0, 100)] public int openness = 50;
    [Range(0, 100)] public int conscientiousness = 50;
    [Range(0, 100)] public int extraversion = 50;
    [Range(0, 100)] public int agreeableness = 50;
    [Range(0, 100)] public int neuroticism = 50;

    public float Openness01 => openness / 100f;
    public float Conscientiousness01 => conscientiousness / 100f;
    public float Extraversion01 => extraversion / 100f;
    public float Agreeableness01 => agreeableness / 100f;
    public float Neuroticism01 => neuroticism / 100f;
}

