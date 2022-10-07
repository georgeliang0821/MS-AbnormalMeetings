import json
import os
 
# Opening JSON file
# json_path = '../src/AbnormalMeetings/local.settings.json'
# originalConfig_path = './originalConfig.json'

json_path = './src/AbnormalMeetings/local.settings.json'
originalConfig_path = './AssistTools/originalConfig.json'
newConfig_path = "./AssistTools/new_config.json"

if not os.path.exists(json_path):
    print(f"{json_path} does not exist. You may in the wrong folder.")
    raise
if not os.path.exists(originalConfig_path):
    print(f"{originalConfig_path} does not exist. You may in the wrong folder.")
    raise
if not os.path.exists(newConfig_path):
    print(f"{newConfig_path} does not exist. You may in the wrong folder.")
    raise

ignore_names = []
# total_dict = {}
new_config = []

with open(originalConfig_path, 'r', encoding='utf-8-sig') as json_file:

    json_file_read = json_file.read()
    if (json_file_read[0] == '"'):
        raise("You should paste your original configuration ")
        
    data = json.loads(json_file_read)
    new_config.extend(data)
    for a_data in new_config:
        ignore_names.append(a_data["name"])    

with open(json_path, 'r', encoding='utf-8-sig') as json_file:

    data = json.loads(json_file.read())
 
    for k, v in data['Values'].items():
        if k in ignore_names: continue

        tmp_dict = {
            "name": k,
            "value": v,
            "slotSetting": new_config[0]["slotSetting"]
        }
        new_config.append(tmp_dict)

with open(newConfig_path, 'w') as f:
    json_string = json.dumps(new_config)
    print("You can now copy and paste the json string below to \"Advanced edit\".")
    print()
    print(json_string)
    f.write(json_string)
