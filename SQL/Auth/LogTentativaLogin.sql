INSERT INTO log_login_tentativas (
    login,
    sucesso,
    ip_address,
    user_agent,
    data_tentativa
) VALUES (
    @login,
    @sucesso,
    @ip_address,
    @user_agent,
    @data_tentativa
);