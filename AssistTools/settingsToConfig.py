import json
 
# Opening JSON file
json_path = '../GetCallRecords_Http/local.settings.json'
originalConfig_path = './originalConfig.json'

ignore_names = []
# total_dict = {}
new_config = []

with open(originalConfig_path, 'r', encoding='utf-8-sig') as json_file:
    data = json.loads(json_file.read())
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

# print(type(new_config[0]))
# print(ignore_names)
with open("new_config.json", 'w') as f:
    json_string = json.dumps(new_config)
    print(json_string)
    f.write(json_string)
