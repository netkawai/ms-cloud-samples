"""
The configuration file would look like this:

{
    "authority": "https://login.microsoftonline.com/organizations",
    "client_id": "your_client_id",
    "scope": ["User.ReadBasic.All"],
        // You can find the other permission names from this document
        // https://docs.microsoft.com/en-us/graph/permissions-reference
    "username": "your_username@your_tenant.com",  // This is optional
    "endpoint": "https://graph.microsoft.com/v1.0/users"
        // You can find more Microsoft Graph API endpoints from Graph Explorer
        // https://developer.microsoft.com/en-us/graph/graph-explorer
}

You can then run this sample with a JSON configuration file:

    python sample.py parameters.json
"""

import sys  # For simplicity, we'll read config file from 1st CLI param sys.argv[1]
import json, logging, msal, requests

# Optional logging
logging.basicConfig(level=logging.DEBUG)  # Enable DEBUG log for entire script
logging.getLogger("msal").setLevel(logging.DEBUG)  # Optionally disable MSAL DEBUG logs


config = json.load(open(sys.argv[1]))

# Create a preferably long-lived app instance which maintains a token cache.
app = msal.PublicClientApplication(
    config["client_id"], authority=config["authority"],
    # token_cache=...  # Default cache is in memory only.
                       # You can learn how to use SerializableTokenCache from
                       # https://msal-python.readthedocs.io/en/latest/#msal.SerializableTokenCache
    )

# The pattern to acquire a token looks like this.
result = None

# Firstly, check the cache to see if this end user has signed in before
accounts = app.get_accounts(username=config.get("username"))
if accounts:
    logging.info("Account(s) exists in cache, probably with token too. Let's try.")
    print("Account(s) already signed in:")
    for a in accounts:
        print(a["username"])
    chosen = accounts[0]  # Assuming the end user chose this one to proceed
    print("Proceed with account: %s" % chosen["username"])
    # Now let's try to find a token in cache for this account
    result = app.acquire_token_silent(config["scope"], account=chosen)

if not result:
    logging.info("No suitable token exists in cache. Let's get a new one from AAD.")
    print("A local browser window will be open for you to sign in. CTRL+C to cancel.")
    result = app.acquire_token_interactive(  # Only works if your app is registered with redirect_uri as http://localhost
        config["scope"],
        login_hint=config.get("username"),  # Optional.
            # If you know the username ahead of time, this parameter can pre-fill
            # the username (or email address) field of the sign-in page for the user,
            # Often, apps use this parameter during reauthentication,
            # after already extracting the username from an earlier sign-in
            # by using the preferred_username claim from returned id_token_claims.

        #prompt=msal.Prompt.SELECT_ACCOUNT,  # Or simply "select_account". Optional. It forces to show account selector page
        #prompt=msal.Prompt.CREATE,  # Or simply "create". Optional. It brings user to a self-service sign-up flow.
            # Prerequisite: https://docs.microsoft.com/en-us/azure/active-directory/external-identities/self-service-sign-up-user-flow
        )

if "access_token" in result:
    # Calling graph using the access token
    graph_response = requests.get(  # Use token to call downstream service
        config["endpoint"],
        headers={'Authorization': 'Bearer ' + result['access_token']},)
    print("Graph API call result: %s ..." % graph_response.text[:100])
else:
    print(result.get("error"))
    print(result.get("error_description"))
    print(result.get("correlation_id"))  # You may need this when reporting a bug

"""OneDrive raw/bear Microsoft Graph access has not test yet
token = result['access_token']
refresh_token = json.loads(response.text)["refresh_token"]

URL = 'https://graph.microsoft.com/v1.0/'
HEADERS = {'Authorization': 'Bearer ' + token}
response = requests.get(URL + 'me/drive/', headers = HEADERS)
if (response.status_code == 200):
    response = json.loads(response.text)
    print('Connected to the OneDrive of', response['owner']['user']['displayName']+' (',response['driveType']+' ).', \
         '\nConnection valid for one hour. Reauthenticate if required.')
elif (response.status_code == 401):
    response = json.loads(response.text)
    print('API Error! : ', response['error']['code'],\
         '\nSee response for more details.')
else:
    response = json.loads(response.text)
    print('Unknown error! See response for more details.')

 Refresh token
def get_refresh_token():
    data = {
        "client_id": client_id,
        "scope": permissions,
        "refresh_token": refresh_token,
        "redirect_uri": redirect_uri,
        "grant_type": 'refresh_token',
        "client_secret": 'xxxx-yyyy-zzzz',
    }

    response = requests.post(URL, data=data)

    token = json.loads(response.text)["access_token"]
    refresh_token = json.loads(response.text)["refresh_token"]
    last_updated = time.mktime(datetime.today().timetuple())

    return token, refresh_token, last_updated

  # List folder
  items = json.loads(requests.get(URL + 'me/drive/root/children', headers=HEADERS).text)
  items = items['value']
  for entries in range(len(items)):
    print(items[entries]['name'], '| item-id >', items[entries]['id'])

  # Upload file
  url = 'me/drive/items/C1465DBECD7188C9!103:/large_file.dat:/createUploadSession'
  url = URL + url
  url = json.loads(requests.post(url, headers=HEADERS).text)
  url = url['uploadUrl']
  file_path = '/local/file/path/large_file.dat'
  file_size = os.path.getsize(file_path)
  chunk_size = 320*1024*10 # Has to be multiple of 320 kb
  no_of_uploads = file_size//chunk_size
  content_range_start = 0
  if file_size < chunk_size :
    content_range_end = file_size
  else :
    content_range_end = chunk_size - 1

  data = open(file_path, 'rb')
  while data.tell() < file_size:
    if ((file_size - data.tell()) <= chunk_size):
        content_range_end = file_size -1
        headers = {'Content-Range' : 'bytes '+ str(content_range_start)+ '-' +str(content_range_end)+'/'+str(file_size)}
        content = data.read(chunk_size)
        response = json.loads(requests.put(url, headers=headers, data = content).text)
    else:
        headers = {'Content-Range' : 'bytes '+ str(content_range_start)+ '-' +str(content_range_end)+'/'+str(file_size)}
        content = data.read(chunk_size)
        response = json.loads(requests.put(url, headers=headers, data = content).text)
        content_range_start = data.tell()
        content_range_end = data.tell() + chunk_size - 1
   data.close()
   response2 = requests.delete(url)
"""