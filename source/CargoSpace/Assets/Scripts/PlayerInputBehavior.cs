using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerInputBehavior : BusParticipant
{
    public GameObject PlayerShip;
    private PlayerShipBehavior _playerShip;

    
    // Start is called before the first frame update
    protected override void InitializeSubscriptions()
    {
        _playerShip = PlayerShip.GetComponent<PlayerShipBehavior>();
        AddLifeTimeSubscription(Subscribe("Input", OnInput));
    }

    private async Task OnInput(Dictionary<string, string> arg)
    {
        if (arg == null)
        {
            //todo: log this
            return;
        }

        const string vertKey = "vert";
        const string horzKey = "horz";
        if (!arg.ContainsKey(vertKey) || !arg.ContainsKey(horzKey))
        {
            //todo: log this
            return;
        }

        if (!float.TryParse(arg[vertKey], out var vert) ||
            !float.TryParse(arg[horzKey], out var horz))
        {
            //todo: log this
            return;
        }

        Vector2 input = new Vector2(horz, vert);
        
        //todo: set this factor via config so that player ship does not know about input scaling
        Vector2 inputFactor = Vector2.one;

        //Vector2 vector = inputFactor * input;

        //_playerShip.Move(vector);
        _playerShip.Move(input);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}