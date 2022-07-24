using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TicTacToeScript : MonoBehaviourPunCallbacks
{
    public GameObject result;
    public GameObject statusText;

    private int _curPlayer = 0;
    private int _myPlayerID;
    private bool _matchStarted = false;

    private int[] _board =
    {
        -1, -1, -1,
        -1, -1, -1,
        -1, -1, -1,
    };

    enum MatchResult
    {
        Win,
        Lose,
        Draw,
        NotDecided,
    }

    string GetMessage(MatchResult result)
    {
        switch (result)
        {
            case MatchResult.Win:
                return "You Win";
            case MatchResult.Lose:
                return "You Lose";
            case MatchResult.Draw:
                return "Draw";
            default:
                return "";
        }
    }
    
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }
    
    bool GetUserInput()
    {
        if (!Input.GetMouseButtonUp(0)) return false;
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider == null) return false;
        
        Vector3 pos = hit.collider.gameObject.transform.position;

        int x = (int) pos.x + 1;
        int y = (int) pos.y + 1;

        int idx = x + y * 3;

        if (_board[idx] != -1) return false;
        
        var prefabName = (_curPlayer == 0) ? "Circle" : "Cross";
        
        PhotonNetwork.Instantiate(prefabName, pos, Quaternion.identity);

        _board[idx] = _curPlayer;
        var hashtable = new Hashtable();
        hashtable["board"] = _board;
        PhotonNetwork.CurrentRoom.SetCustomProperties(hashtable);
        return true;

    }

    private MatchResult JudgeResult()
    {
        List<int[]> lines = new List<int[]>();
        lines.Add(new int[]{0, 1, 2});
        lines.Add(new int[]{3, 4, 5});
        lines.Add(new int[]{6, 7, 8});
        lines.Add(new int[]{0, 3, 6});
        lines.Add(new int[]{1, 4, 7});
        lines.Add(new int[]{2, 5, 8});
        lines.Add(new int[]{0, 4, 8});
        lines.Add(new int[]{2, 4, 6});
        
        foreach (var line in lines)
        {
            if (_board[line[0]] == -1) continue;
            if (_board[line[0]] == _board[line[1]]
                && _board[line[1]] == _board[line[2]])
                return (_board[line[0]] == _myPlayerID) ? MatchResult.Win : MatchResult.Lose;
        }
        
        foreach (var cell in _board)
        {
            if (cell == -1) return MatchResult.NotDecided;
        }

        return MatchResult.Draw;
    }

    void Update()
    {
        if (!_matchStarted) return;

        bool isMyTurn = _curPlayer == _myPlayerID;
        statusText.GetComponent<TextMeshProUGUI>().text = isMyTurn ? "Your turn" : "Enemy's turn";
        if (!isMyTurn) return;
        
        bool placed = GetUserInput();
        if (!placed) return;
        
        
        Debug.Log("Placed!!");

        var matchResult = JudgeResult();

        if (matchResult == MatchResult.NotDecided)
        {
            _curPlayer = (_curPlayer + 1) % 2;
            var hashtable = new Hashtable();
            hashtable["curPlayer"] = _curPlayer;
            PhotonNetwork.CurrentRoom.SetCustomProperties(hashtable);
        }
        else
        {
            result.SetActive(true);
            statusText.GetComponent<TextMeshProUGUI>().text = GetMessage(matchResult);
            _matchStarted = false;
        }
    }
    
    public override void OnConnectedToMaster() {
        PhotonNetwork.JoinOrCreateRoom("Room", new RoomOptions(), TypedLobby.Default);
    }

    public override void OnJoinedRoom() {
        if (PhotonNetwork.IsMasterClient)
        {
            statusText.GetComponent<TextMeshProUGUI>().text = "Waiting...";
            _myPlayerID = 0;
            Debug.Log("Join room as master");
        }
        else
        {
            statusText.GetComponent<TextMeshProUGUI>().text = "";
            _myPlayerID = 1;
            Debug.Log("Join room as slave");
            _matchStarted = true;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            statusText.GetComponent<TextMeshProUGUI>().text = "";
            _matchStarted = true;
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        _board = (PhotonNetwork.CurrentRoom.CustomProperties["board"] is int[] v1) ? v1 : _board;
        _curPlayer = (PhotonNetwork.CurrentRoom.CustomProperties["curPlayer"] is int v2) ? v2 : _curPlayer;

        var matchResult = JudgeResult();

        if (matchResult != MatchResult.NotDecided)
        {
            result.SetActive(true);
            statusText.GetComponent<TextMeshProUGUI>().text = GetMessage(matchResult);
            _matchStarted = false;
        }
    }
}
