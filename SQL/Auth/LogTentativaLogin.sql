INSERT INTO logLoginTentativas (
    login,
    sucesso,
    ipaddress,
    useragent,
    datatentativa
) VALUES (
    @login,
    @sucesso,
    @ipaddress,
    @useragent,
    @datatentativa
);