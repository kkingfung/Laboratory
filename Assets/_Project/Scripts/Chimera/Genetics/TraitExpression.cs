using UnityEngine;

namespace Laboratory.Chimera.Genetics
{
    /// <summary>
    /// Represents the expression level and characteristics of a genetic trait
    /// </summary>
    [System.Serializable]
    public class TraitExpression
    {
        public string traitName;
        public float expressionLevel;
        public bool isDominant;
        public TraitType traitType;
        public float stability;
        public float dominanceStrength;
        
        // Properties for compatibility
        public string TraitName 
        {
            get => traitName;
            set => traitName = value;
        }
        
        public float Value 
        {
            get => expressionLevel;
            set => expressionLevel = value;
        }
        
        public float DominanceStrength 
        {
            get => dominanceStrength;
            set => dominanceStrength = value;
        }
        
        public float ExpressionLevel 
        {
            get => expressionLevel;
            set => expressionLevel = value;
        }
        
        public TraitExpression(string name, float level, TraitType type)
        {
            traitName = name;
            expressionLevel = level;
            traitType = type;
            isDominant = level > 0.5f;
            stability = Random.Range(0.7f, 1.0f);
            dominanceStrength = isDominant ? Random.Range(0.6f, 0.9f) : Random.Range(0.1f, 0.4f);
        }
        
        public TraitExpression Clone()
        {
            return new TraitExpression(traitName, expressionLevel, traitType)
            {
                isDominant = this.isDominant,
                stability = this.stability,
                dominanceStrength = this.dominanceStrength
            };
        }
    }
}