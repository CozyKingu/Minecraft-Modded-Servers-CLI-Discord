# Minecraft-Modded-Servers-CLI-Discord

An all-in-one hybrid command-line and Discord bot tool to manage modded Minecraft servers!  
Easily create servers, manage configurations, and sync mods and assets without messy `.minecraft` folders.

---

## Commands Documentation

### 1. create-server
**Description:** Creates a new Minecraft server.  
**Parameters:**  
- serverName: The name of the server.  
- configName: The configuration to use.

**Example:**  
`/create-server myServer myConfig`

---

### 2. create-config
**Description:** Creates a new server configuration.  
**Parameters:**  
- configName: The name of the configuration.  
- modLoader: The mod loader (e.g., Forge).  
- version: The Minecraft version.

**Example:**  
`/create-config myConfig Forge 1.19.4`

---

### 3. status-server
**Description:** Checks the status of a server.  
**Parameters:**  
- serverName: The name of the server.

**Example:**  
`/status-server myServer`

---

### 4. down-server
**Description:** Stops a running server.  
**Parameters:**  
- serverName: The name of the server.

**Example:**  
`/down-server myServer`

---

### 5. up-server
**Description:** Starts a server.  
**Parameters:**  
- serverName: The name of the server.  
- port: The port number.

**Example:**  
`/up-server myServer 25565`

---

### 6. remove-server
**Description:** Removes a server.  
**Parameters:**  
- serverName: The name of the server.

**Example:**  
`/remove-server myServer`

---

### 7. add-mod
**Description:** Adds a mod to a configuration.  
**Parameters:**  
- configName: The configuration name.  
- modName: The name of the mod.  
- link: The URL to the mod.  
- clientSide (optional): Boolean for client-side mod.  
- serverSide (optional): Boolean for server-side mod.

**Example:**  
`/add-mod myConfig myMod http://mod-link.com true false`

---

### 8. remove-config
**Description:** Removes a configuration.  
**Parameters:**  
- configName: The name of the configuration.

**Example:**  
`/remove-config myConfig`

---

### 9. remove-mod
**Description:** Removes a mod from a configuration.  
**Parameters:**  
- configName: The configuration name.  
- modName: The name of the mod.

**Example:**  
`/remove-mod myConfig myMod`

---

### 10. add-plugin
**Description:** Adds a plugin to a configuration.  
**Parameters:**  
- configName: The configuration name.  
- pluginName: The name of the plugin.  
- link: The URL to the plugin.

**Example:**  
`/add-plugin myConfig myPlugin http://plugin-link.com`

---

### 11. add-resource-pack
**Description:** Adds a resource pack to a configuration.  
**Parameters:**  
- configName: The configuration name.  
- resourcePackName: The name of the resource pack.  
- link: The URL to the resource pack.  
- serverDefault (optional): Boolean for default server resource pack.

**Example:**  
`/add-resource-pack myConfig myPack http://pack-link.com true`

---

### 12. remove-plugin
**Description:** Removes a plugin from a configuration.  
**Parameters:**  
- configName: The configuration name.  
- pluginName: The name of the plugin.

**Example:**  
`/remove-plugin myConfig myPlugin`

---

### 13. add-server-mod
**Description:** Adds a mod to a server.  
**Parameters:**  
- serverName: The name of the server.  
- modName: The name of the mod.  
- link: The URL to the mod.

**Example:**  
`/add-server-mod myServer myMod http://mod-link.com`

---

### 14. add-server-plugin
**Description:** Adds a plugin to a server.  
**Parameters:**  
- serverName: The name of the server.  
- pluginName: The name of the plugin.  
- link: The URL to the plugin.

**Example:**  
`/add-server-plugin myServer myPlugin http://plugin-link.com`

---

### 15. remove-server-mod
**Description:** Removes a mod from a server.  
**Parameters:**  
- serverName: The name of the server.  
- modName: The name of the mod.

**Example:**  
`/remove-server-mod myServer myMod`

---

### 16. remove-server-plugin
**Description:** Removes a plugin from a server.  
**Parameters:**  
- serverName: The name of the server.  
- pluginName: The name of the plugin.

**Example:**  
`/remove-server-plugin myServer myPlugin`

---

### 17. set-server-property
**Description:** Sets a specific property for a server.  
**Parameters:**  
- serverName: The name of the server.  
- keyValue: The key-value pair for the property.

**Example:**  
`/set-server-property myServer max-players=20`

---

## Notes

- **Standalone Clients:** Based on [MultiMC](https://github.com/MultiMC) for easy Minecraft modded client distribution.  
- **Development Status:** Documentation and releases are being prepared. Stay tuned for updates!
- **Mod loaders:** Compatible with [Forge](https://github.com/minecraftforge) and [NeoForge](https://github.com/neoforged/NeoForge)

---

## License

This project is licensed under the MIT license. See the `LICENSE` file for more information.
