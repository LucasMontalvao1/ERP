CREATE TABLE IF NOT EXISTS usuario_roles (
    usuario_id INT NOT NULL,
    role_id INT NOT NULL,
    data_atribuicao DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (usuario_id, role_id),
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
);