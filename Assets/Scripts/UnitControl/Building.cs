using UnityEngine;
using UnityEngine.UI;

public class Building : MonoBehaviour
{
    public int hasBeenClaimed = 0; // 0 = unclaimed, 1 = player, 2 = enemy
    public bool isBase = false;
    public GameObject outline;
    public TileData tile = null;

    public void UpdateState()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        switch(hasBeenClaimed)
        {
            case 0:
                sr.sprite = Resources.Load<Sprite>("Resource-Neutral");
                break;
            case 1:
                if(isBase) sr.sprite = Resources.Load<Sprite>("Tower-Cat");
                else sr.sprite = Resources.Load<Sprite>("Resource-Cat");
                break;
            case 2:
                if(isBase) sr.sprite = Resources.Load<Sprite>("Tower-Dog");
                else sr.sprite = Resources.Load<Sprite>("Resource-Dog");
                break;
        }
    }

    public void SetOutline(bool state)
    {
        if(outline == null) outline = transform.Find("Outline").gameObject;
        outline.SetActive(state);
    }
}
