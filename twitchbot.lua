local cmdPath = "D:/_/..Projects/.Edits/.Stream/SMB3/script/CSharpBot/SMB3Bot/luaCMD.txt";
local socket = require("socket.core");

local readFrame = false

function sleep(sec)
    socket.select(nil, nil, sec)
end

function readCommandFile()
	if readFrame then
		emu.log("reading")
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
	else
		emu.log("NOT reading")
	end
	readFrame = not readFrame
end

emu.addEventCallback(readCommandFile, emu.eventType.endFrame);

emu.displayMessage("Script", "Twitch Bot Lua script loaded.")
