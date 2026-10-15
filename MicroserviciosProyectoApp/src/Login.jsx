import { useState, useEffect } from 'react';
import Modal from 'react-bootstrap/Modal';
import FloatingLabel from 'react-bootstrap/FloatingLabel';
import Button from 'react-bootstrap/Button';
import Col from 'react-bootstrap/Col';
import Row from 'react-bootstrap/Row';
import Form from 'react-bootstrap/Form';
import api from './utils/api';

function Login() {
    const [email, setEmail] = useState('');
    const [pass, setPass] = useState('');

    function login() {
      api.post('/auth/login', {
        email: email,
        password: pass
      })
      .then((response) => {
        localStorage.setItem('access_token', response.data.accessToken);
        localStorage.setItem('refresh_token', response.data.refreshToken);
        console.log(response.data);
      })
      .catch((error) => {
        console.error(error);
      })
      .finally(() => {
        console.log('Completado');
      });
    }

    return (
      <Modal show={true} centered>
          <Modal.Header>
              <Modal.Title>App Perrona</Modal.Title>
          </Modal.Header>
          <Modal.Body>
            <Row>
                <Col>
                    <FloatingLabel label="E-mail" className="mb-3">
                        <Form.Control type="email" placeholder="correo@ejemplo.com" value={email} onChange={e => setEmail(e.target.value)} />
                    </FloatingLabel>
                </Col>
            </Row>
            <Row>
                <Col>
                    <FloatingLabel label="Contraseña" className="mb-3">
                        <Form.Control type="password" placeholder="contraseña" value={pass} onChange={e => setPass(e.target.value)} />
                    </FloatingLabel>
                </Col>
            </Row>
          </Modal.Body>
          <Modal.Footer>
            <Button onClick={() => login()}>Ingresar</Button>
          </Modal.Footer>
        </Modal>
    );
}

export default Login;