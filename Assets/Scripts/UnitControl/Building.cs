using UnityEngine;

public class Building : MonoBehaviour
{
    public int hasBeenClaimed = 0; // 0 = unclaimed, 1 = player, 2 = enemy
    public void UpdateState()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        switch(hasBeenClaimed)
        {
            case 0:
                sr.color = Color.black;
                break;
            case 1:
                sr.color = Color.blue;
                break;
            case 2:
                sr.color = Color.red;
                break;
        }
    }
}
