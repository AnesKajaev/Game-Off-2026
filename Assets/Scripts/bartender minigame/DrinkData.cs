using UnityEngine;

[CreateAssetMenu(menuName = "Bartender/Drink")]
public class DrinkData : ScriptableObject
{
    public string drinkName = "Vodka";
    public Sprite bottleSprite;
    public Glass glassPrefab;
    public Color liquidColor = Color.white;

    [Header("Pouring")]
    public Vector2 mouthLocal = new Vector2(0f, 1f);
    public float pourAngle = 100f; 
    public float maxTilt = 150f;
    public float minFlow = 8f;    
    public float maxFlow = 50f;     

    [Header("Scoring")]
    public int points = 100;
}