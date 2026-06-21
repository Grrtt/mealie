ARG VARIANT="1-10.0-bookworm"
FROM mcr.microsoft.com/devcontainers/dotnet:${VARIANT}

ARG NODE_VERSION="none"
RUN if [ "${NODE_VERSION}" != "none" ]; then su vscode -c "umask 0002 && . /usr/local/share/nvm/nvm.sh && nvm install ${NODE_VERSION} 2>&1"; fi

RUN echo "export PROMPT_COMMAND='history -a'" >> /home/vscode/.bashrc \
    && echo "export HISTFILE=~/commandhistory/.bash_history" >> /home/vscode/.bashrc \
    && chown vscode:vscode -R /home/vscode/

RUN npm install -g @go-task/cli yarn@1.22.22

RUN apt-get update \
    && apt-get install --no-install-recommends -y \
    curl \
    ffmpeg \
    gnupg gnupg2 gnupg1 \
    python3 \
    python3-pip \
    && rm -rf /var/lib/apt/lists/*

RUN pip3 install --no-cache-dir --break-system-packages ingredient-parser-nlp \
    && python3 -c "from ingredient_parser import parse_ingredient; parse_ingredient('1 cup water')"

RUN mkdir -p /run/secrets
