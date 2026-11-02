using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    InputAction moveAction;
    InputAction jumpAction;
    InputAction menuAction;
    Rigidbody _rigidbody;
    public float _thrust;
    public float _jumpSpeed;
    public float _rotationSpeed = 10f;

    public int wood_amount = 0;

    private NPC _target;
    private Vector3 direction;

    private bool _showInventory;

    public Animator mAnimator;

    public GameObject textPanel;
    public GameObject menuPanel;

    [SerializeField]
    public Dictionary<Item,int> _inventory = new Dictionary<Item,int>();

    private void Start()
    {
        //get the right components from the player object
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        menuAction = InputSystem.actions.FindAction("Crouch");
        _rigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {

        mAnimator.SetBool("walking", false);
        //see if moveAction isnt zero and add as force the players rigidbody
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        if (moveValue != Vector2.zero)
        {
            var bod = this;

            direction = new Vector3(moveValue.x,0,moveValue.y);

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            bod.transform.rotation = Quaternion.RotateTowards(
                bod.transform.rotation,
                targetRotation,
                _rotationSpeed
            );

            _rigidbody.AddForce(bod.transform.forward * _thrust);
            mAnimator.SetBool("walking", true);
        }

        if((_target != null) && _target.CanSee())
        {
            textPanel.SetActive(true);
            _showInventory = false;
        } else {
            textPanel.SetActive(false);
        }

        if(_showInventory)
        {
            textPanel.SetActive(true);
            textPanel.transform.Find("Portrait").GetComponent<Image>().sprite = null;
            textPanel.transform.Find("Body").GetComponent<TMP_Text>().text = "Wood: " + wood_amount;
            textPanel.transform.Find("Title").GetComponent<TMP_Text>().text = "Inventory";
        }


    }

    void Update()
    {
        if(jumpAction.WasPressedThisFrame())
        {
            _showInventory = !_showInventory;
        }
        if(menuAction.WasPressedThisFrame())
        {
            menuPanel.SetActive(!menuPanel.activeSelf);
        }

    }

    public void Give(Item item)
    {
        if (!_inventory.ContainsKey(item))
        {
            _inventory.Add(item,0);
        }
        _inventory[item] += 5;
        wood_amount = _inventory[item];
    }

    public void SetTarget(NPC npc)
    {

            _target = npc;
            textPanel.transform.Find("Portrait").GetComponent<Image>().sprite = npc.person.portrait;
            textPanel.transform.Find("Body").GetComponent<TMP_Text>().text = npc.person.chatter;
            textPanel.transform.Find("Title").GetComponent<TMP_Text>().text = npc.person.longname;
    }
}
