from python:3.9-slim-bullseye
workdir /app

# system tools
run apt-get update && apt-get install -y git build-essential

# install dependencies
copy requirements.txt .
run pip install --upgrade pip && \
    pip install mlagents mlagents-envs && \
    pip install torch>=2.1.0 protobuf==3.20.3 six && \
    pip install -r requirements.txt

copy . .

cmd ["mlagents-learn", "config.yaml", "--run-id=spa_training_01"]