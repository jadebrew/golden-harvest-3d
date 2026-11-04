using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    InputAction moveAction;
    InputAction jumpAction;
    InputAction menuAction;
    InputAction nextAction;
    InputAction previousAction;
    InputAction interactAction;

    Rigidbody _rigidbody;
    public float _thrust;
    public float _jumpSpeed;
    public float _rotationSpeed = 10f;

    public int wood_amount = 0;

    private NPC _target;
    private GameObject _targetObject;
    private Vector3 direction;

    public GameObject itemSlot;

    private bool _showMenu;

    public Animator mAnimator;

    public GameObject heldModel;
    public Item equippedItem;

    public Sprite defaultIcon;

    public GameObject textPanel;
    public GameObject craftingContainer;
    public GameObject craftingPanel;
    public List<GameObject> menuTabs;
    private int tabIndex;

    private Dictionary<AuraType,float> environmentBuffs = new Dictionary<AuraType,float>();

    [SerializeField]
    public Dictionary<Item,int> _inventory = new Dictionary<Item,int>();

    public List<Recipe> knownRecipes;
    private bool inMenu = true;

    public GameObject buttonPrefab;
    public GameObject inventoryContainer;
    private bool faceTarget = false;

    private void Start()
    {
        //get the right components from the player object
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        menuAction = InputSystem.actions.FindAction("Crouch");
        nextAction = InputSystem.actions.FindAction("Next");
        previousAction = InputSystem.actions.FindAction("Previous");
        interactAction = InputSystem.actions.FindAction("Interact");
        _rigidbody = GetComponent<Rigidbody>();

        closeMenu();
        Equip();
    }

    void Equip()
    {
        var container = itemSlot;
        foreach(Transform child in container.transform)
        {
            Destroy(child.gameObject);
        }
        Instantiate(heldModel,itemSlot.transform);
    }

    void RotateTowards(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x,0,direction.z));

        var bod = this;

        bod.transform.rotation = Quaternion.RotateTowards(
            bod.transform.rotation,
            targetRotation,
            _rotationSpeed
        );
    }

    void FixedUpdate()
    {

        mAnimator.SetBool("walking", false);
        //see if moveAction isnt zero and add as force the players rigidbody
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        if (moveValue != Vector2.zero)
        {
            var bod = this;

           // direction = new Vector3(moveValue.x,0,moveValue.y);
            //RotateTowards(direction);


            _rigidbody.AddForce(bod.transform.forward * moveValue.y * _thrust);
            mAnimator.SetBool("walking", true);
            Vector3 _AngleVelocity = new Vector3(0, moveValue.x*_rotationSpeed, 0);
            Quaternion deltaRotation = Quaternion.Euler(_AngleVelocity * Time.fixedDeltaTime);
            _rigidbody.MoveRotation(_rigidbody.rotation * deltaRotation);
        }

        if((_target != null) && _target.CanSee())
        {
            textPanel.SetActive(true);
        } else {
            textPanel.SetActive(false);
        }

        if (faceTarget)
        {
            RotateTowards(_targetObject.transform.position - transform.position);
        }

        updateInventory();
    }

    IEnumerator UseDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        var node = _targetObject.GetComponent<Node>();
        if (node != null)
        {
            if (node.InRange() && node.vulnerability == equippedItem.damageType)
            {
                node.health -= equippedItem.damage;

            }
        }
        faceTarget = false;
    }

    void Update()
    {
        if(jumpAction.WasPressedThisFrame())
        {
            if (equippedItem != null)
            {
                mAnimator.SetTrigger("sweep");
                if (_targetObject != null)
                {
                    faceTarget = true;
                    StartCoroutine(UseDelay(0.7f));
                }
            }

        }
        if(interactAction.WasPressedThisFrame())
        {
            if (!_showMenu)
            {
                showCraftMenu();
            }
            else
            {
                closeMenu();
                _showMenu = false;
            }

        }
        if(menuAction.WasPressedThisFrame())
        {
            if (!_showMenu)
            {
                showMenu(0);
                _showMenu = true;
            } else {
                closeMenu();
                _showMenu = false;
            }

        }
        if(nextAction.WasPressedThisFrame())
        {
            if (_showMenu)
            {
                tabIndex++;
                showMenu(tabIndex);
            }
        }
        if(previousAction.WasPressedThisFrame())
        {
            if (_showMenu)
            {
                tabIndex--;
                showMenu(tabIndex);
            }
        }

    }

    void showMenu(int index)
    {
        tabIndex = index;
        closeMenu();
        if (tabIndex>=menuTabs.Count)
        {
            tabIndex = 0;
        } else if (tabIndex < 0)
        {
            tabIndex = menuTabs.Count-1;
        }
        updateInventory();
        menuTabs[tabIndex].SetActive(true);
    }

    void updateInventory()
    {
        var container = inventoryContainer;
        foreach(Transform child in container.transform)
        {
            Destroy(child.gameObject);
        }

        foreach(KeyValuePair<Item,int> item in _inventory)
        {
            var button = Instantiate(buttonPrefab,container.transform);
            button.transform.Find("Icon").GetComponent<Image>().sprite = item.Key.sprite;
            button.transform.Find("Label").GetComponent<TMP_Text>().text = item.Value.ToString();
        }
    }

    void showCraftMenu()
    {
        craftingPanel.SetActive(true);
        _showMenu = true;

        var container = craftingContainer;
        foreach(Transform child in container.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (Recipe _recipe in knownRecipes)
        {

            var text = "";
            Sprite icon = defaultIcon;
            bool CanCraft = true;
            foreach (KeyValuePair<Item,int> _item in _recipe.output)
            {
                //text = text + _item.Key.itemName;
                icon =  _item.Key.sprite;

            }
            foreach (KeyValuePair<Item,int> _item in _recipe.input)
            {
                text = text + _item.Value + "x " + _item.Key.itemName + "\n";
                if (!_inventory.ContainsKey(_item.Key))
                {
                    CanCraft = false;
                } else {
                    if(_inventory[_item.Key] < _item.Value)
                    {
                        CanCraft = false;
                    }
                }
            }
            var button = Instantiate(buttonPrefab,container.transform);
            button.transform.Find("Icon").GetComponent<Image>().sprite = icon;
            button.transform.Find("Label").GetComponent<TMP_Text>().text = text;
            button.GetComponent<Button>().onClick.AddListener(() => CraftItem(_recipe));
            if(!CanCraft)
            {
                 button.GetComponent<Button>().interactable = false;
            }
        }
    }

    public void CraftItem(Recipe recipe)
    {
        foreach (KeyValuePair<Item,int> _item in recipe.input)
        {
           Remove(_item.Key,_item.Value);
        }
        foreach (KeyValuePair<Item,int> _item in recipe.output)
        {
            Give(_item.Key,_item.Value);
        }
        showCraftMenu();
    }

    void closeMenu()
    {
        foreach (GameObject menu in menuTabs)
        {
            menu.SetActive(false);
        }
        craftingPanel.SetActive(false);
        _showMenu = false;
    }

    public void Give(Item item, int amount)
    {
        if (!_inventory.ContainsKey(item))
        {
            _inventory.Add(item,0);
        }
        _inventory[item] += amount;
    }

    public void Remove(Item item, int amount)
    {
        if (!_inventory.ContainsKey(item))
        {
            _inventory.Add(item,0);
        }
        _inventory[item] -= amount;
    }

    public void SetTarget(NPC npc)
    {

            _target = npc;
            textPanel.transform.Find("Portrait").GetComponent<Image>().sprite = npc.person.portrait;
            textPanel.transform.Find("Body").GetComponent<TMP_Text>().text = npc.person.chatter;
            textPanel.transform.Find("Title").GetComponent<TMP_Text>().text = npc.person.longname;
    }

    public void SetTarget(GameObject target)
    {
        _targetObject = target;
    }
}
