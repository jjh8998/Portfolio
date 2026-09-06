using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackAniScript : MonoBehaviour
{

    public static PlayerAttackAniScript instance;

    [SerializeField]
    private Animator heroAnim;
    [SerializeField]
    private Animator effectAnim;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetIsHeroAttack(bool _Bool)
    {
        heroAnim.SetBool("isHeroAttack", _Bool);
    }

    public void SetIsSlashAttack(bool _Bool)
    {
        effectAnim.SetBool("isSlashAttack", _Bool);
    }
}
