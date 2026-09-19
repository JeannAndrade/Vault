# Sobre o uso do MariaDb

## Como criar o container local

O container do MariaDb deve ser criado adicionando as variáveis de ambiente que criarão o banco de dados e o usuário transacional.

```bash
docker run --name vault-local \
  -e MARIADB_ROOT_PASSWORD=$VAULT_DB_ROOT \
  -e MARIADB_DATABASE=$VAULT_DB_NAME \
  -e MARIADB_USER=$VAULT_DB_USER \
  -e MARIADB_PASSWORD=$VAULT_DB_PASS \
  -p 3307:3306 \
  -v vault-db-data:/var/lib/mariaDb \
  -d mariadb:latest
```

Criado o banco já com o usuário, já é possível atualizar o banco com o Migration.

## Verificando banco via docker

```bash
docker exec -it vault-local mariadb -u$VAULT_DB_USER -p$VAULT_DB_PASS $VAULT_DB_NAME
```

## Para exibir o resultado

Para ver as tabelas criadas:

`SHOW TABLES;`

Para ver as colunas de uma tabela:

`DESCRIBE MinhaTabela;`
