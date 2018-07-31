function date_time(id)
{
	date = new Date;
	year = date.getFullYear();
	month = date.getMonth();
	months = new Array('Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho', 'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro');
	d = date.getDate();
	day = date.getDay();
	days = new Array('Domingo', 'Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta', 'Sábado');

	h = date.getHours();
	if(h<10)
	{
			h = "0"+h;
	}

	m = date.getMinutes();
	if(m<10)
	{
			m = "0"+m;
	}

	s = date.getSeconds();
	if(s<10)
	{
			s = "0"+s;
	}

    result = '' + days[day] + ' ' + months[month] + ' ' + d + ' ' + year + ' ' + h + ':' + m + ':' + s;
	document.getElementById(id).innerHTML = result;
	setTimeout('date_time("'+id+'");','1000');
	return true;
}