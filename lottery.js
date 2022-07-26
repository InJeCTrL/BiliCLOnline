self.onmessage = function (event) {
    let data = event.data;
    let result = {"data": [], "count": 0};
	
	let filtered = [];
	let uidSet = new Set();
	
	for (let i = 0; i < data.data.length; ++i) {
		if (data.onlySpecified && data.data[i].content.contains(data.contentSpecified)){
			continue;
		}
		
		let pubTime = new Date(data.data[i].pubTime);
		if (data.limitTime && pubTime >= data.startDateTime && pubTime <= data.endDateTime){
			continue;
		}
		
		if (data.duplicatedUID){
			if (uidSet.has(data.data[i].uID)){
				continue;
			}
			else{
				uidSet.add(data.data[i].uID);
			}
		}
		
		filtered.add(data.data[i]);
	}
	
	if (filtered.length < data.count){
		throw "符合条件的评论少于中奖人数";
	}
	
	result.count = data.count;
	
	for (let i = filtered.length; i > filtered.length - data.count; --i){
		let rnd = Math.floor(Math.random() * i);
		result.data.add(filtered[rnd]);
		filtered[rnd] = filtered[i];
	}
	
    this.postMessage(result);
};