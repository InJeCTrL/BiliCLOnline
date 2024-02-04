self.onmessage = function (event) {
    let data = event.data;
    let result = {"data": [], "count": 0, "err": false, "msg": ""};
	
	let filtered = [];
	let twiceFiltered = [];
	let uidSet = new Set();

	let replySet = new Set();
	if (data.deDuplicatedReply){
		data.data.sort(function(x, y){
			return new Date(x.pubTime) - new Date(y.pubTime);
		});
	}
	
	for (let i = 0; i < data.data.length; ++i) {
		if (data.onlySpecified && !data.data[i].content.includes(data.contentSpecified)){
			continue;
		}
		
		let pubTime = new Date(data.data[i].pubTime);
		if (data.limitTime && (pubTime < data.startDateTime || pubTime > data.endDateTime)){
			continue;
		}
		
		if (!data.levels.has(data.data[i].level)){
			continue;
		}
		
		filtered.push(data.data[i]);
	}

	for (let i = 0; i < filtered.length; ++i) {
		if (!data.duplicatedUID){
			if (uidSet.has(filtered[i].uid)){
				continue;
			} else {
				uidSet.add(filtered[i].uid);
			}
		}

		if (data.deDuplicatedReply){
			if (replySet.has(filtered[i].content)){
				continue;
			} else {
				replySet.add(filtered[i].content);
			}
		}

		twiceFiltered.push(filtered[i]);
	}
	
	if (twiceFiltered.length < data.count){
		result.err = true;
		result.msg = "符合条件的评论少于中奖人数";
		this.postMessage(result);
	}
	
	result.count = data.count;
	
	for (let i = twiceFiltered.length; i > twiceFiltered.length - data.count; --i){
		let rnd = Math.floor(Math.random() * i);
		result.data.push(twiceFiltered[rnd]);
		twiceFiltered[rnd] = twiceFiltered[i - 1];
	}
	
    this.postMessage(result);
};