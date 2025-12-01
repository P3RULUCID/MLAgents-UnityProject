# Unity ML-Agents: Intelligent Navigation

An AI navigation system trained with reinforcement learning to autonomously navigate around moving obstacles and reach dynamic targets. Built with Unity ML-Agents and PPO algorithm, achieving **90% success rate** with efficient pathfinding in ~250 steps per episode.

**DEMO** Videos located in folder for visual of live-time running agents!

## Quick Start

**Prerequisites**: Unity 2021.3+, Python 3.9+ (Updated versions may work with minor issues)

```bash
# Clone and setup
git clone https://github.com/YOUR_USERNAME/YOUR_REPO_NAME.git
cd YOUR_REPO_NAME
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate
pip install mlagents==1.0.0

# Open in Unity and press Play to see trained agents in action!
# To train: mlagents-learn config/navigation_config.yaml --run-id=MyRun
