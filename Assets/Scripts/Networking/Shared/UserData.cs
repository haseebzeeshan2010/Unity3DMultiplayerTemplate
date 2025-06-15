using UnityEngine;
using System;

[System.Serializable] // Allows this class to be serialized(converted to a byte array).
public class UserData
{
    public string username;
    public string userAuthId; // This is the ID of the user in the authentication system.
}
