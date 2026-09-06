using System;
using System.Collections.Generic;

[Serializable]
public class CardPackOpenResult
{
    public bool success;
    public string errorMessage;
    public string packId;
    public int drawCount;
    public List<string> gainedCardIds = new List<string>();
}
