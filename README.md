# ZA Shiny Warper

用于在 **《宝可梦传说：Z-A》** 中进行角色**传送（Warping）**或**闪光猎捕（Shiny Hunting）** 的工具。

> ⚠️ 需要一台运行 [sys-botbase](https://github.com/olliz0r/sys-botbase/releases) 或 [usb-botbase](https://github.com/Koi-3088/USB-Botbase/releases) 的已破解 Nintendo Switch 主机.

![Main window](ZAShinyWarper_Window.png?raw=true "The program itself")
![Success window](ZAShinyWarper_Success.png?raw=true "Finding a shiny")

## ⚙️ 闪光猎捕设置步骤
1) 确保你的 Switch 正在运行 Atmosphère，并已安装 sys-botbase 或类似工具，然后使用 **JKSV** 等软件创建游戏存档备份。  
2) 打开本程序并连接到主机，你将看到所有“已储存的闪光宝可梦（stashed shinies）”。  
3) 找到一个或多个附近的宝可梦刷新点（spawn locations）：  
   - 对于每个位置，点击 **「Set Pos（设置位置）」**；  
   - 然后稍微离开该区域；  
   - 再点击 **「Restore Pos（恢复位置）」** 来验证该位置是否稳定。  
4) 设置一个最终的「安全位置」，应靠近但几乎没有宝可梦刷新、可以方便保存游戏的地方，并进行同样的验证。  
5) （可选）设置想要寻找的闪光宝可梦筛选条件。建议在软件初期阶段保持默认不变。
6) 点击 **「Begin Warping（开始传送）」**。

程序将执行以下操作：

- 旋转相机并在所有记录的位置间传送 10 次；  
- 然后保存游戏以生成“闪光储藏”（shiny stash）。
- 
接着它会读取储藏数据，如果检测到闪光宝可梦，则会**暂停游戏并显示提示**。

> ⚠️ 你需要亲自去捕捉闪光宝可梦，本工具不会自动捕捉。  

若未找到闪光，程序会自动循环执行，直到找到闪光或你手动停止。

所有从“闪光储藏”中发现的宝可梦都会以**未完成（野生状态）**的形式保存到 `StashedShinies` 文件夹中。

## 🧩 已知问题

- 本项目使用的内存指针目前较为稳定，但仍存在更理想的指针位置，尤其是在无需保存游戏即可进行闪光储藏的情况下——这部分还需要进一步研究。  
- 有时角色可能会**飞上天**，机器人会自动修复这一问题。  
- 如果某个位置位于**战斗区域**，机器人最终会提示无法传送的错误。  
- 代码写得很乱（原作者自嘲 😅）。  
- 在 PKHeX 尚未支持《Z-A》之前，工具会将宝可梦数据保存为 `.PK9` 文件，而非 Z-A 专用格式。

## 🧭 其他用途

- 快速传送到附近位置。  
- 多次从建筑上掉下或传送回屋顶。  
- 传送进地图几何结构内并[掉出地图走向“必然的毁灭”](https://x.com/berichandev/status/1980471677659279623)（由于碰撞体加载距离问题，过远传送时必然会发生）。

🎥 演示视频：[https://youtu.be/eKydGGQbS_0](https://youtu.be/eKydGGQbS_0)

## 🙏 致谢

本项目基于 [Kurt](https://github.com/kwsch) 的 [NHSE.Injection](https://github.com/kwsch/NHSE) 构建，并使用 **PKHeX nuget** 与 **sys-botbase 接口**。  
感谢 **Anubis** 的[研究推文](https://x.com/Sibuna_Switch/status/1980306261213393163)，为本项目提供了起点。  
感谢 **Olliz0r、Koi** 和 **FishGuy** 提供的接口工具支持。

本人项目仅进行汉化工作，项目作者：[berichan](https://github.com/berichan)
