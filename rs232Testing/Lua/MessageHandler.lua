msg = ''
count = 0
stats = {}
rec = nil

function data_received(data)
	msg = string.format("%s%s",msg,data)
	local found = msg:match("(MSG#%a*%d+999)")
	if found then 
		msg = ''
		message_received(found) 
	end
end

function message_received(message)
	if not rec then 
		rec = clr.rs232Testing.Record()
		rec.StartTime = clr.System.DateTime.Now 
	end
	count = count + 1
	rec.Messages.Add(message)
	if count == 10 then		
		rec.Span = clr.System.DateTime.Now.Subtract(rec.StartTime)
		stats[#stats + 1] = rec.Span
		Save(rec)
		count = 0 
		rec = nil
		print(string.format("%s", stats[#stats]))
	end
	--print('PING')
	--print('message: ' .. message)
end

function end_program()
	print("Number of stats: %d", #stats )
end