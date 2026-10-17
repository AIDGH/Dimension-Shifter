"""Original synthesized ambient cues and paired music loops, generated for this project.
No third-party music samples. Existing Kenney sword slice is layered at playback.
"""
from pathlib import Path
import wave
import numpy as np
root=Path(__file__).resolve().parents[2]
sr=22050
rng=np.random.default_rng(261016)
def write(path,data,peak=.8):
    path.parent.mkdir(parents=True,exist_ok=True)
    data=np.asarray(data,dtype=np.float64)
    data-=data.mean(axis=0)
    data*=peak/max(float(np.abs(data).max()),1e-9)
    with wave.open(str(path),'wb') as f:
        f.setnchannels(1 if data.ndim==1 else data.shape[1]);f.setsampwidth(2);f.setframerate(sr);f.writeframes((np.clip(data,-1,1)*32767).astype('<i2').tobytes())
def air(t,width=24):
    noise=rng.normal(size=len(t));return np.convolve(noise,np.ones(width)/width,mode='same')
def cue(name,duration,data):
    fade=np.minimum(1,np.arange(len(data))/100)*np.minimum(1,np.arange(len(data))[::-1]/220)
    write(root/'Assets/Resources/SFX'/f'{name}_0.wav',data*fade,.7)
for name,duration in [('dash',.48),('jump',.23),('air_jump',.3),('hurt',.24),('player_death',.7),('shift',.6),('pickup',.48),('checkpoint',1.5)]:
    t=np.arange(int(sr*duration))/sr
    if name=='dash':
        env=np.sin(np.pi*np.clip(t/duration,0,1))**1.2
        data=air(t,12)*env*2+air(t,60)*env+np.sin(2*np.pi*(850*t-650*t*t))*np.exp(-t*16)*.06
    elif name in ('jump','air_jump'):
        env=np.sin(np.pi*t/duration)**1.4
        data=air(t,32)*env*.7+np.sin(2*np.pi*(180*t+620*t*t))*env*.18
    elif name in ('hurt','player_death'):
        phase=2*np.pi*(105*t+55*(1-np.exp(-t*18))/18)
        data=np.sin(phase)*np.exp(-t*(18 if name=='hurt' else 5))*.4+air(t,22)*np.exp(-t*26)*.7
    else:
        notes={'shift':[294,440,587],'pickup':[587,880,1175],'checkpoint':[294,370,440,587]}[name]
        data=np.zeros(len(t))
        for i,f in enumerate(notes):
            u=t-i*.075;env=np.where(u>=0,(1-np.exp(-np.maximum(0,u)*120))*np.exp(-np.maximum(0,u)*5),0)
            data+=(np.sin(2*np.pi*f*u)+.18*np.sin(2*np.pi*f*2.01*u))*env*.15
    cue(name,duration,data)
# D minor / Bb major / F major / C suspended, with a restrained original bell line.
length=32;n=sr*length
chords=[[50,57,65],[46,53,62],[41,53,60],[48,55,62]]
melody=[74,69,77,76,74,70,65,69,72,77,76,69,74,72,67,69]
for world in ['Frost','Echo']:
    mix=np.zeros((n,2));echo=world=='Echo'
    def add(signal,start,pan):
        idx=(np.arange(len(signal))+int(start*sr))%n
        np.add.at(mix[:,0],idx,signal*np.sqrt((1-pan)/2));np.add.at(mix[:,1],idx,signal*np.sqrt((1+pan)/2))
    for j,chord in enumerate(chords):
        t=np.arange(sr*10)/sr;env=np.sin(np.pi*t/10)**2
        for k,midi in enumerate(chord):
            f=440*2**((midi-69)/12);tone=np.sin(2*np.pi*f*t)+.24*np.sin(2*np.pi*f*2*t)+.08*np.sin(2*np.pi*f*3*t)
            if echo:tone=.65*np.sin(2*np.pi*f*.5*t)+.35*tone
            add(tone*env*.09,j*8-1,(-.5+k*.5))
    for j,midi in enumerate(melody):
        t=np.arange(sr*4)/sr;f=440*2**((midi-69)/12);env=(1-np.exp(-t*60))*np.exp(-t*(1.8 if echo else 2.5))
        tone=(np.sin(2*np.pi*f*t)+.22*np.sin(2*np.pi*f*2.003*t)+.09*np.sin(2*np.pi*f*4.01*t))*env*.055
        add(tone,j*2+.3,(-.4 if j%2 else .4))
    # Circular reverb preserves loop continuity.
    dry=mix.copy()
    for delay,gain in [(0.19,.25),(.37,.2),(.71,.16),(1.13,.1)]:mix+=np.roll(dry,int(sr*delay),axis=0)*gain
    write(root/'Assets/Resources/Music'/f'{world}.wav',mix,.55)
print('Created 8 original action/UI cues and two synchronized 32-second ambient loops.')
