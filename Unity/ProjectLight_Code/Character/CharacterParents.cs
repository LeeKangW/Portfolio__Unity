﻿using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(CapsuleCollider2D))]
public class CharacterParents : MonoBehaviour
{
    [Header("enum")]
    [SerializeField] protected ENUM_Character.Kinds kinds = ENUM_Character.Kinds.None;

    [SerializeField] protected ENUM_Character.States state = ENUM_Character.States.CanPlayed;

    [Header("Speed")]
    [Range(0.1f, 10.0f)]
    [SerializeField] protected float speed = 3.0f;

    [Range(0.1f, 10.0f)]
    [SerializeField] protected float jumpSpeed = 5.0f;

    [Header("Component")]
    [SerializeField] protected Animator anim = null;

    [SerializeField] protected Rigidbody2D rigid = null;
    [SerializeField] protected Transform trans = null;

    protected virtual void Awake()
    {
        anim = this.GetComponent<Animator>();
        rigid = this.GetComponent<Rigidbody2D>();
        trans = this.GetComponent<Transform>();
    }
}