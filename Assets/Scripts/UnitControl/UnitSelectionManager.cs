using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance;
    public GameObject unitActionUIPrefab;

    private enum State { NoSelection, UnitSelected, SelectingMovement, SelectingAttack }

    private Unit currentHover = null;
    private Unit currentSelected = null;
    private GameObject currentUI = null;
    private State currentState = State.NoSelection;

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        switch(currentState)
        {
            case State.NoSelection:
                NoSelection();
                break;
            case State.UnitSelected:
                UnitSelected();
                break;
            case State.SelectingMovement:
                //SelectingMovement();
                break;
            case State.SelectingAttack:
                //SelectingAttack();
                break;
        }
    }

    private void HandleHover()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        Unit unitHit = null;
        if(hit.collider != null) unitHit = hit.collider.GetComponent<Unit>(); 

        bool hoverChanged = unitHit != currentHover;

        if(currentHover != null && currentHover != currentSelected && hoverChanged)
        {
            currentHover.SetOutline(false);
        }
        if(hoverChanged) currentHover = unitHit;
        if(currentHover != null && currentHover.IsSelectable() && currentHover != currentSelected)
        {
            currentHover.SetOutline(true);
        }
    }

    private void NoSelection()
    {
        HandleHover();

        if (currentHover != null && currentHover.IsSelectable() && Input.GetMouseButtonDown(0))
        {
            currentState = State.UnitSelected;
            currentSelected = currentHover;
        }
    }
    
    private void UnitSelected()
    {
        HandleHover();

        if(EventSystem.current.IsPointerOverGameObject()) return;

        if( ( currentHover == null || currentHover == currentSelected) && Input.GetMouseButtonDown(0))
        {
            currentState = State.NoSelection;
            currentSelected.SetOutline(false);
            currentSelected = null;
            if (currentUI != null) 
            {
                Destroy(currentUI);
                currentUI = null;
            }
        }  
        else if(currentHover != null && currentHover.IsSelectable() && Input.GetMouseButtonDown(0))
        {
            currentSelected.SetOutline(false);
            currentSelected = currentHover;
            Destroy(currentUI);
            currentUI = null;
        }

        if (currentUI == null && currentSelected != null) currentUI = Instantiate(unitActionUIPrefab, currentSelected.transform);
        if(currentUI != null)
        {
            Transform canvas = currentUI.transform.Find("Canvas");
            Button attackBtn = canvas.Find("AttackButton").GetComponent<Button>();
            Button moveBtn = canvas.Find("MoveButton").GetComponent<Button>();
        }
    }
    /*
    private void SelectingMovement()
    {
        if(currentSelected != null) currentSelected.SetOutline(true);
        Debug.Log("EligiendoMovimiento");

        if (!IsMoveButtonPressed())
        {
            currentState = State.UnitSelected;
            moveBtn.interactable = true;
        }
    }

    private void SelectingAttack()
    {
        if(currentSelected != null) currentSelected.SetOutline(true);

        Debug.Log("EligiendoAtaque");

        if (!IsAttackButtonPressed())
        {
            currentState = State.UnitSelected;
            attackBtn.interactable = true;
        }
    }

    private bool IsMoveButtonPressed()
    {
        if (currentUI == null) return false;
        moveBtn = currentUI.transform.Find("Canvas").GetComponent<Button>();
        return moveBtn != null && moveBtn.IsInteractable();
    }

    private bool IsAttackButtonPressed()
    {
        if (currentUI == null) return false;
        attackBtn = currentUI.transform.Find("Canvas").GetComponent<Button>();
        return attackBtn != null && attackBtn.IsInteractable();
    }
    */
}
