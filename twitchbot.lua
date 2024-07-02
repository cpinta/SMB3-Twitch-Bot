local cmdPath = "D:/_/..Projects/.Edits/.Stream/SMB3/script/CSharpBot/SMB3Bot/luaCMD.txt";
local socket = require("socket.core");

function sleep(sec)
    socket.select(nil, nil, sec)
end

function readCommandFile()
	local file = assert(io.open(cmdPath, "r"));
	local filestring = file:read("*all");
	local newFileString = "";
	local i = 0;
	for str in string.gmatch(filestring, "[^\r\n]+") do
		if i == 0 then
			f = assert(load(str))();
		else
			newFileString = newFileString .. "\r\n".. str
		end
		i= i+1;
	end
	file = io.open(cmdPath, "w")
	file:write(newFileString);
	file:close();
end

emu.addEventCallback(readCommandFile, emu.eventType.endFrame);

emu.displayMessage("Script", "Twitch Bot Lua script loaded.")
