# Sobre o uso do MariaDb

Este documento descreve o banco **local de desenvolvimento** (usado com `dotnet run`, porta 3307).
Para subir o sistema inteiro em containers, veja [Docker.md](Docker.md); o compose tem o seu próprio banco, que não publica porta.

## Como criar o container local

O container do MariaDb deve ser criado adicionando as variáveis de ambiente que criarão o banco de dados e o usuário transacional.
O diretório de dados da imagem é `/var/lib/mysql`; é nele que o volume precisa ser montado.

```bash
docker run --name vault-local \
  -e MARIADB_ROOT_PASSWORD=$VAULT_DB_ROOT \
  -e MARIADB_DATABASE=$VAULT_DB_NAME \
  -e MARIADB_USER=$VAULT_DB_USER \
  -e MARIADB_PASSWORD=$VAULT_DB_PASS \
  -p 3307:3306 \
  -v vault-db-data:/var/lib/mysql \
  -d mariadb:12.3.3
```

Criado o banco já com o usuário, já é possível atualizar o banco com o Migration.

## Se o seu container antigo foi criado com o volume em `/var/lib/mariaDb`

Versões anteriores deste documento montavam o volume no caminho errado, então os dados ficaram num volume anônimo
preso ao container. Para não perdê-los antes de recriar:

```bash
docker exec vault-local sh -c 'mariadb-dump -uroot -p"$MARIADB_ROOT_PASSWORD" --databases "$MARIADB_DATABASE"' > backup.sql
docker rm -f vault-local
# recrie com o comando acima e restaure:
docker exec -i vault-local sh -c 'mariadb -uroot -p"$MARIADB_ROOT_PASSWORD"' < backup.sql
```

## Verificando banco via docker

```bash
docker exec -it vault-local mariadb -u$VAULT_DB_USER -p$VAULT_DB_PASS $VAULT_DB_NAME
```

## Para exibir o resultado

Para ver as tabelas criadas:

`SHOW TABLES;`

Para ver as colunas de uma tabela:

`DESCRIBE MinhaTabela;`
