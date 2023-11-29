import sys
import websocket
import importlib
import json
from nltk import edit_distance
from time import sleep
from pprint import pprint
from functools import partial

if __name__ == "__main__":
    ws = websocket.create_connection('ws://localhost:3000/metroSim')
    filename = sys.argv[1]
    file=open(filename,"r")
    rawData = file.read();
    data = json.loads(rawData)
    logs = data["time_steps"]
    index = 0
    command = {
            "command":"set_games",
            "args":{
                "games":[]
                }
            }

    command["args"]["games"] = logs[0]["games"]
    ws.send(json.dumps(command))



    #Send games
    games = '''
    {
        "command":"set_games", 
        "args":{
            "games":[
                {"score":0,"time":12.99741,"isPause":false,"isGameover":false,"freeTrains":1,"stations":[{"id":0,"unique_id":-199052,"type":"station","shape":"Cube","x":0,"y":1,"z":2,"timer":0,"human_name":"1"},{"id":1,"unique_id":-199354,"type":"station","shape":"Cone","x":-0.03771269,"y":0.5,"z":1.948061,"timer":0,"human_name":"2"},{"id":2,"unique_id":-199660,"type":"station","shape":"Sphere","x":-0.7451621,"y":0.5,"z":1.780211,"timer":0,"human_name":"3"}],"lines":[{"id":0,"unique_id":-199956,"type":"line"},{"id":1,"unique_id":-199962,"type":"line"},{"id":2,"unique_id":-199968,"type":"line"}],"trains":[{"unique_id":-200410,"type":"train","position":0.4501298,"speed":0.001687572,"direction":-1,"line_id":-199956},{"unique_id":-200582,"type":"train","position":0.3960411,"speed":0.002248887,"direction":-1,"line_id":-199962},null],"segments":[{"type":"segment","length":20,"which_line":-199956,"from_station":-199052,"to_station":-199354},{"type":"segment","length":20,"which_line":-199956,"from_station":-199354,"to_station":-199660},{"type":"segment","length":20,"which_line":-199962,"from_station":-199660,"to_station":-199052}]},
                {"score":0,"time":0.9999159,"isPause":false,"isGameover":false,"freeTrains":3,"stations":[{"id":0,"unique_id":-199052,"type":"station","shape":"Cube","x":0,"y":1,"z":2,"timer":0,"human_name":"1"},{"id":1,"unique_id":-199354,"type":"station","shape":"Cone","x":-0.03771269,"y":0.5,"z":1.948061,"timer":0,"human_name":"2"},{"id":2,"unique_id":-199660,"type":"station","shape":"Sphere","x":-0.7451621,"y":0.5,"z":1.780211,"timer":0,"human_name":"3"}],"lines":[{"id":0,"unique_id":-199956,"type":"line"},{"id":1,"unique_id":-199962,"type":"line"},{"id":2,"unique_id":-199968,"type":"line"}],"trains":[null,null,null],"segments":[]}
            ]
        }
    }
    '''
    games2 = '''
    {
        "command":"set_games", 
        "args":{
            "games":[
                {"score":0,"time":0.9999159,"isPause":false,"isGameover":false,"freeTrains":3,"stations":[{"id":0,"unique_id":-199052,"type":"station","shape":"Cube","x":0,"y":1,"z":2,"timer":0,"human_name":"1"},{"id":1,"unique_id":-199354,"type":"station","shape":"Cone","x":-0.03771269,"y":0.5,"z":1.948061,"timer":0,"human_name":"2"},{"id":2,"unique_id":-199660,"type":"station","shape":"Sphere","x":-0.7451621,"y":0.5,"z":1.780211,"timer":0,"human_name":"3"}],"lines":[{"id":0,"unique_id":-199956,"type":"line"},{"id":1,"unique_id":-199962,"type":"line"},{"id":2,"unique_id":-199968,"type":"line"}],"trains":[null,null,null],"segments":[]},
                {"score":0,"time":12.99741,"isPause":false,"isGameover":false,"freeTrains":1,"stations":[{"id":0,"unique_id":-199052,"type":"station","shape":"Cube","x":0,"y":1,"z":2,"timer":0,"human_name":"1"},{"id":1,"unique_id":-199354,"type":"station","shape":"Cone","x":-0.03771269,"y":0.5,"z":1.948061,"timer":0,"human_name":"2"},{"id":2,"unique_id":-199660,"type":"station","shape":"Sphere","x":-0.7451621,"y":0.5,"z":1.780211,"timer":0,"human_name":"3"}],"lines":[{"id":0,"unique_id":-199956,"type":"line"},{"id":1,"unique_id":-199962,"type":"line"},{"id":2,"unique_id":-199968,"type":"line"}],"trains":[{"unique_id":-200410,"type":"train","position":0.4501298,"speed":0.001687572,"direction":-1,"line_id":-199956},{"unique_id":-200582,"type":"train","position":0.3960411,"speed":0.002248887,"direction":-1,"line_id":-199962},null],"segments":[{"type":"segment","length":20,"which_line":-199956,"from_station":-199052,"to_station":-199354},{"type":"segment","length":20,"which_line":-199956,"from_station":-199354,"to_station":-199660},{"type":"segment","length":20,"which_line":-199962,"from_station":-199660,"to_station":-199052}]}
            ]
        }
    }
    '''

    while True:
        tries = 0
        while tries < 3:
            try:
                data = ws.recv()
                break
            except:
                print("recieve failed trying again")
                tries += 1
                ws = websocket.create_connection('ws://localhost:3000/metroSim')
                ws.send(noop)
        else:
            print("something went wrong")
            #loop did not break
            tries = 0
            while tries < 3:
                try:
                    ws.send(noop)
                    break
                except:
                    ws = websocket.create_connection('ws://localhost:3000/metroSim')
                    tries += 1
            else:
                print("something went really really wrong")
                #something went wrong... quit
            continue
                


        info = json.loads(data)

        if info["Status"] == "Complete":
            pprint(info)
            index += 1
            if index >= len(logs):
                break
            command["args"]["games"] = logs[index]["games"]
            while True:
                try:
                    ws.send(json.dumps(command))
                    break
                except:
                    print("sending failed, trying again")
                    ws = websocket.create_connection('ws://localhost:3000/metroSim')
        else:
            pprint(info)
            noop = '{"command":"noop"}'
            ws.send(noop)

        sleep(2)

    print("Done simulating")

