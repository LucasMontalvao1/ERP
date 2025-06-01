CREATE TABLE IF NOT EXISTS roles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(50) NOT NULL UNIQUE,
    descricao VARCHAR(255) NULL,
    ativo TINYINT(1) DEFAULT 1,
    INDEX idx_nome (nome),
    INDEX idx_ativo (ativo)
);
